using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using SpendingCoach.Models;
using System.IO;

namespace SpendingCoach.Controllers
{
    public class WizardController : CentralController
    {
        // GET: Wizard
        public ActionResult Start()
        {
            if (Request.IsAuthenticated)
            {
                Guid CustomerId = Guid.Parse(User.Identity.GetUserId());
                WizardViewModel MyWizardViewModel = new WizardViewModel();
                GetWizardViewModel(MyWizardViewModel, CustomerId);
                return View(MyWizardViewModel);
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
            
        }

        [HttpPost]
        public ActionResult Start(WizardViewModel MyWizardViewModel)
        {
            if (Request.IsAuthenticated)
            {
                Guid CustomerId = Guid.Parse(User.Identity.GetUserId());
                var Status = SaveWizardInstance(MyWizardViewModel, CustomerId);
                ModelState.Clear();
                MyWizardViewModel = GetWizardViewModel(MyWizardViewModel, CustomerId);
                return View(MyWizardViewModel);
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
            
        }

        public Boolean SaveWizardInstance(WizardViewModel MyWizardViewModel, Guid CustomerId)
        {
            try
            {
                Guid BudgetId = GetBudgetId(CustomerId);
                var Context = new Central();
                var MyWizardInstance = (from b in Context.WizardInstances
                                       where b.BudgetId == BudgetId
                                       select b).FirstOrDefault();
                Guid WizardId = MyWizardInstance.WizardId;
                MyWizardInstance.Position = MyWizardViewModel.WizardPosition;
                MyWizardInstance.PrevPosition = MyWizardViewModel.WizardPrevPosition;

                //Info
                MyWizardInstance.PrimaryName = MyWizardViewModel.PrimaryName;
                MyWizardInstance.SpouseName = MyWizardViewModel.SpouseName;

                

                //Answers
                MyWizardInstance.InitialPurpose = MyWizardViewModel.Answers.InitialPurpose;
                MyWizardInstance.SubsequentPurpose = MyWizardViewModel.Answers.SubsequentPurpose;
                MyWizardInstance.MostImportantBudgetItem = MyWizardViewModel.Answers.MostImportantItem;
                MyWizardInstance.UpdatedDate = DateTime.Now;
                
                //Save Changes to Wizard Instance in Database
                Context.Entry(MyWizardInstance).State = System.Data.Entity.EntityState.Modified;
                Context.SaveChanges();


                
                if (MyWizardViewModel.BudgetItems != null)
                {
                    //Save any new Budget Items
                    foreach (var x in MyWizardViewModel.BudgetItems.NewBudgetItems)
                    {
                        if (x.BudgetListItemAmount > 0)
                        {
                            var NewItemStatus = EnterNewBudgetItem(x, CustomerId, "0");
                        }
                    }

                    //Save any Existing Budget Items
                    var ExistingItemStatus = EditBudgetItemList(MyWizardViewModel.BudgetItems.ExistingBudgetItems, CustomerId);

                }
                

                //Save Employers
                if (MyWizardViewModel.UserEmployers != null)
                {
                    Boolean SavedEmployers = SaveUserEmployers(MyWizardViewModel.UserEmployers, WizardId, Context);
                }
                
                
                return true;
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
                return false;
            }
        }

        public WizardViewModel GetWizardViewModel(WizardViewModel MyWizardViewModel, Guid CustomerId)
        {
            try
            {
                if (!HasBudget(CustomerId))
                {
                    CreateNewBudget(CustomerId);
                }
                Guid BudgetId = GetBudgetId(CustomerId);
                if (!HasWizard(BudgetId))
                {
                    CreateNewWizard(BudgetId);
                }
                var Context = new Central();
                var MyWizardInstance = (from b in Context.WizardInstances
                                        where b.BudgetId == BudgetId
                                        select b).FirstOrDefault();
                Guid WizardId = MyWizardInstance.WizardId;
                string Position = MyWizardInstance.Position;
                MyWizardViewModel.WizardPosition = Position;
                MyWizardViewModel.WizardPrevPosition = MyWizardInstance.PrevPosition;
                
                //Get Info
                MyWizardViewModel.PrimaryName = MyWizardInstance.PrimaryName;
                MyWizardViewModel.SpouseName = MyWizardInstance.SpouseName;

                //Get Employers
                MyWizardViewModel.UserEmployers = new UserEmployerLists();
                MyWizardViewModel.UserEmployers.ExistingEmployers = new List<UserEmployer>();
                MyWizardViewModel.UserEmployers.NewEmployers = new List<UserEmployer>();
                var MyWizardEmployers = from b in Context.WizardEmployers
                                        where b.WizardId == WizardId
                                        select b;
                //iterate over any existing employers
                foreach (var x in MyWizardEmployers)
                {
                    UserEmployer MyEmployer = new UserEmployer();
                    MyEmployer.UserEmployerId = x.EmployerId;
                    MyEmployer.UserEmployerName = x.EmployerName;
                    MyEmployer.UserEmployerType = x.EmployerType;
                    MyEmployer.UserEmployerUser = x.EmployerUser;
                    MyWizardViewModel.UserEmployers.ExistingEmployers.Add(MyEmployer);
                }

                //Add new Employers to potentially add
                for (var x = 0; x < 5; x++)
                {
                    UserEmployer MyEmployer = new UserEmployer();
                    MyWizardViewModel.UserEmployers.NewEmployers.Add(MyEmployer);
                }


                    //Get all the Wizard Answers
                    MyWizardViewModel.Answers = new WizardAnswers();
                MyWizardViewModel.Answers.InitialPurpose = MyWizardInstance.InitialPurpose;
                MyWizardViewModel.Answers.SubsequentPurpose = MyWizardInstance.SubsequentPurpose;
                MyWizardViewModel.Answers.MostImportantItem = MyWizardInstance.MostImportantBudgetItem;

                //Get all the helpers for the answers
                MyWizardViewModel.Helpers = new WizardHelpers();

                    //Get helpers for Most Important Budget Item Section
                    MyWizardViewModel.Helpers.MostImportantBudgetItems = new List<MostImportantBudgetItem>();
                    var MyWizardMostImportantBudgItem = (from b in Context.L_MostImportantBudgetItemQuizItem
                                                         select b);
                    foreach (var x in MyWizardMostImportantBudgItem)
                    {
                        MyWizardViewModel.Helpers.MostImportantBudgetItems.Add(new MostImportantBudgetItem { MostImportantItemId = x.QuizItemId, MostImportantItemName = x.QuizItemName });
                    }
                
                
                //Get Budget Items
                MyWizardViewModel.BudgetItems = GetBudgetItems(CustomerId, Context);

                
                
                var MyWizardMapping = (from b in Context.L_WizardMapping
                                       where b.WizardPage == Position
                                       select b).FirstOrDefault();
                MyWizardViewModel.WizardPageMap = new WizardMap();
                MyWizardViewModel.WizardPageMap.WizardMapNextFirst = MyWizardMapping.WizardFirstNextPage;
                MyWizardViewModel.WizardPageMap.WizardMapNextSecond = MyWizardMapping.WizardSecondNextPage;
                MyWizardViewModel.WizardPageMap.WizardMapNextThird = MyWizardMapping.WizardThirdNextPage;
                MyWizardViewModel.WizardPageMap.WizardMapPrevFirst = MyWizardMapping.WizardFirstPrevPage;
                MyWizardViewModel.WizardPageMap.WizardMapPrevSecond = MyWizardMapping.WizardSecondPrevPage;
                MyWizardViewModel.WizardPageMap.WizardMapPrevThird = MyWizardMapping.WizardThirdPrevPage;

                var MyWizardPossiblePositions = (from b in Context.L_WizardMapping
                                                 select b.WizardPage).ToList();

                MyWizardViewModel.PossiblePositions = MyWizardPossiblePositions;

                


                MyWizardViewModel.SuccessMessage = "Success";
                return MyWizardViewModel;
            }
            catch (Exception e)
            {
                MyWizardViewModel.ErrorMessage = e.ToString();
                return MyWizardViewModel;
            }
            

            
        }

        public Boolean HasWizard(Guid BudgetId)
        {
            try
            {
                var Context = new Central();
                var WizardInstance = (from b in Context.WizardInstances
                                      where b.BudgetId == BudgetId
                                      select b).FirstOrDefault();
                if (WizardInstance != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
                return false;
            }
        }

        public Boolean CreateNewWizard(Guid BudgetId)
        {
            try
            {
                var Context = new Central();
                WizardInstance NewWizardInstance = new WizardInstance();
                NewWizardInstance.WizardId = Guid.NewGuid();
                NewWizardInstance.BudgetId = BudgetId;
                NewWizardInstance.Position = "010101";
                NewWizardInstance.CreationDate = DateTime.Now;
                NewWizardInstance.UpdatedDate = DateTime.Now;
                Context.WizardInstances.Add(NewWizardInstance);
                Context.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Console.Write(e.ToString());
                return false;
            }
        }

        public BudgetItemLists GetBudgetItems(Guid CustomerId, Central Context)
        {
            //Get the Budget ID based off of the Customer Id
            var BudgetId = GetBudgetId(CustomerId);

            //Establish BudgetItem Lists to Return
            BudgetItemLists BudgetItems = new BudgetItemLists();
            BudgetItems.ExistingBudgetItems = new List<BudgetListItem>();
            BudgetItems.NewBudgetItems = new List<BudgetListItem>();

            //Get Suggested Items
            var SuggestedItems = (from c in Context.L_SuggestedBudgetItem
                                  select c);

            //Loop through Suggested Items
            foreach (var x in SuggestedItems)
            {
                //get all Budget Items for current Customer where BudgetItemName is the same
                BudgetListItem NewBudgetItem = new BudgetListItem();
                NewBudgetItem.BudgetListItemName = x.BudgetItemName;
                NewBudgetItem.BudgetListItemCategory = x.BudgetItemCategory;
                BudgetItems.NewBudgetItems.Add(NewBudgetItem);
            }
                
            //Get all existing Budget Items
            var CurrentBudgetItems = (from b in Context.BudgetItems
                                          where b.BudgetId == BudgetId
                                          select b);
  
            //Foreach matched Budget Item (if any) create a new Budget Item and add it to the main return Budget Item List
            foreach (var y in CurrentBudgetItems)
            {
                //Add Budget Item to Budget Item List
                BudgetListItem NewBudgetItem = new BudgetListItem();
                NewBudgetItem.BudgetListItemAmount = y.BudgetItemAmount;
                NewBudgetItem.BudgetListItemCategory = y.BudgetItemCategory;
                NewBudgetItem.BudgetListItemFrequency = y.BudgetItemFrequency;
                NewBudgetItem.BudgetListItemId = y.BudgetItemId.ToString();
                NewBudgetItem.BudgetListItemName = y.BudgetItemName;
                NewBudgetItem.BudgetListItemType = y.BudgetItemType;
                BudgetItems.ExistingBudgetItems.Add(NewBudgetItem);
                    
                 
            }
                  
            //Return List of Budget Items
            return BudgetItems;
        }


        //Save User Employers
        public Boolean SaveUserEmployers(UserEmployerLists MyUserEmployers, Guid WizardId, Central Context)
        {
            try
            {
                if (MyUserEmployers.ExistingEmployers != null)
                {
                    foreach (var x in MyUserEmployers.ExistingEmployers)
                    {
                        var EmployerInstanceToUpdate = (from b in Context.WizardEmployers
                                                        where b.EmployerId == x.UserEmployerId
                                                        select b).FirstOrDefault();
                        if (EmployerInstanceToUpdate.EmployerName != x.UserEmployerName)
                        {
                            EmployerInstanceToUpdate.EmployerName = x.UserEmployerName;
                            EmployerInstanceToUpdate.UpdatedDate = DateTime.Now;
                            Context.Entry(EmployerInstanceToUpdate).State = System.Data.Entity.EntityState.Modified;
                        }
                    }
                }
                if (MyUserEmployers.NewEmployers != null)
                {
                    foreach (var x in MyUserEmployers.NewEmployers)
                    {
                        if (x.UserEmployerName != null)
                        {
                            WizardEmployer MyWizardEmployer = new WizardEmployer();
                            MyWizardEmployer.EmployerId = Guid.NewGuid();
                            MyWizardEmployer.WizardId = WizardId;
                            MyWizardEmployer.EmployerName = x.UserEmployerName;
                            MyWizardEmployer.EmployerType = x.UserEmployerType;
                            MyWizardEmployer.EmployerUser = x.UserEmployerUser;
                            MyWizardEmployer.CreationDate = DateTime.Now;
                            MyWizardEmployer.UpdatedDate = DateTime.Now;
                            Context.WizardEmployers.Add(MyWizardEmployer);
                        }
                    }
                }
                Context.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }
    }
}