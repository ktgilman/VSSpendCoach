using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using SpendingCoach.Models;

namespace SpendingCoach.Controllers
{
    public class CentralController : Controller
    {
        // GET: Central
        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
            {
                return View(); 
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
            
        }


        public ActionResult Budget(string message)
        {
            if (Request.IsAuthenticated)
            {
                Guid CustomerId = new Guid();
                CustomerId = Guid.Parse(User.Identity.GetUserId());
                if (HasBudget(CustomerId))
                {
                    BudgetViewModel UseBudgetViewModel = CreateRealBudgetViewModel(CustomerId);
                    UseBudgetViewModel.SuccessMessage = message;
                    return View(UseBudgetViewModel);
                }
                else
                {
                    BudgetViewModel NewBudgetViewModel = CreateFirstBudgetViewModel();
                    return View(NewBudgetViewModel);
                }
                
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpPost]
        public ActionResult Budget(BudgetViewModel MyBudgetViewModel)
        {
            if(Request.IsAuthenticated)
            {
                Guid CustomerId = Guid.Parse(User.Identity.GetUserId());
                if(ModelState.IsValid)
                {
                    
                    if (HasBudget(CustomerId) == false)
                    {
                        string EnteredNewBudget = CreateNewBudget(CustomerId);
                        if(EnteredNewBudget == "Success")
                        {
                            MyBudgetViewModel.HasBudget = true;
                        }
                        else
                        {
                            MyBudgetViewModel.ErrorMessage = EnteredNewBudget;
                        }
                        return View(MyBudgetViewModel);
                    }
                    else
                    {
                        if (MyBudgetViewModel.NewBudgetItem != null)
                        {
                            if (HasBudgetItem(CustomerId, MyBudgetViewModel.NewBudgetItem.BudgetListItemName))
                            {
                                BudgetViewModel ErrorBudgetViewModel = CreateRealBudgetViewModel(CustomerId);
                                ErrorBudgetViewModel.ErrorMessage = "Your budget already has the Budget Item - " + MyBudgetViewModel.NewBudgetItem.BudgetListItemName;
                                ErrorBudgetViewModel.NewBudgetItem = MyBudgetViewModel.NewBudgetItem;
                                ErrorBudgetViewModel.InputType = MyBudgetViewModel.InputType;
                                return View(ErrorBudgetViewModel);
                            }
                            string EnteredItem = EnterNewBudgetItem(MyBudgetViewModel.NewBudgetItem, CustomerId, MyBudgetViewModel.InputType);
                            BudgetViewModel UseBudgetViewModel = CreateRealBudgetViewModel(CustomerId);
                            if (EnteredItem == "Success")
                            {
                                UseBudgetViewModel.SuccessMessage = "New Budget Item has successfully been entered";
                                return View(UseBudgetViewModel);
                            }
                            else
                            {
                                UseBudgetViewModel.ErrorMessage = EnteredItem;
                                return View(UseBudgetViewModel);
                            }
                        }
                        else
                        {
                            var EditStatus = EditBudgetItemList(MyBudgetViewModel.BudgetList, CustomerId);
                            BudgetViewModel UseBudgetViewModel = CreateRealBudgetViewModel(CustomerId);
                            if (EditStatus == "Success")
                            {
                                UseBudgetViewModel.SuccessMessage = "Budget Items have successfully been edited";
                            }
                            else
                            {
                                UseBudgetViewModel.ErrorMessage = EditStatus;
                            }
                            return View(UseBudgetViewModel);
                        }
                        
                    }
                }
                else
                {
                    if (HasBudget(CustomerId))
                    {
                        BudgetViewModel UseBudgetViewModel = CreateRealBudgetViewModel(CustomerId);
                        UseBudgetViewModel.NewBudgetItem = MyBudgetViewModel.NewBudgetItem;
                        UseBudgetViewModel.InputType = MyBudgetViewModel.InputType;
                        return View(UseBudgetViewModel);
                    }
                    else
                    {
                        return View(MyBudgetViewModel);
                    }
                    
                }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Delete(BudgetNameToDelete ItemToDelete)
        {
            string result = "";
            try
            {
                Guid CustomerId = Guid.Parse(User.Identity.GetUserId());
                var Context = new Central();
                Guid BudgetId = GetBudgetId(CustomerId);
                var ToDelete = (from b in Context.BudgetItems
                                where b.BudgetItemName == ItemToDelete.DeleteName && b.BudgetId == BudgetId
                                select b).FirstOrDefault();
                Context.BudgetItems.Remove(ToDelete);
                Context.SaveChanges();
                result = "You have successfully deleted the Budget Item - " + ItemToDelete.DeleteName;
                
            }
            catch(Exception e)
            {
                result = e.ToString();
            }
            return RedirectToAction("Budget", "Central",new{message = result});
        }

        public BudgetViewModel CreateFirstBudgetViewModel()
        {
            BudgetViewModel myBudget = new BudgetViewModel();
            myBudget.HasBudget = false;
            return myBudget;
        }

        public BudgetViewModel CreateRealBudgetViewModel(Guid CustomerId)
        {
            BudgetViewModel myBudget = new BudgetViewModel();
            try 
	        {	    
                var context = new Central();
                var Budg = (from b in context.Budgets
                             where b.CustomerId == CustomerId
                             select new { BudgId = b.BudgetId, Position = b.Position }).FirstOrDefault();
                Guid BudgId = Budg.BudgId;
                var BudgItems = (from b in context.BudgetItems
                                where b.BudgetId == BudgId
                                orderby b.BudgetItemType
                                select new BudgetListItem{ BudgetListItemId = b.BudgetItemId.ToString(), BudgetListItemName = b.BudgetItemName, BudgetListItemAmount = b.BudgetItemAmount, BudgetListItemCategory = b.BudgetItemCategory,
                                                BudgetListItemFrequency = b.BudgetItemFrequency, BudgetListItemType = b.BudgetItemType}).ToList();
                foreach (var x in BudgItems)
                {
                    x.BudgetListItemAmount = Decimal.Round(x.BudgetListItemAmount, 2, MidpointRounding.AwayFromZero);
                }

                var Categories = from b in context.L_Category
                                 orderby b.CategoryOrder
                                 select new SelectListItem { Value = b.CategoryName, Text = b.CategoryName };
                IEnumerable<SelectListItem> SuggBudgItems = Enumerable.Empty<SelectListItem>().AsQueryable();
                foreach (var x in Categories)
                {
                    SelectListGroup CategoryGroup = new SelectListGroup();
                    CategoryGroup.Name = x.Text;
                    var BudgetItemList = (from b in context.L_SuggestedBudgetItem
                                          where b.BudgetItemCategory == x.Text
                                          select new { SuggBudgName = b.BudgetItemName}).ToList();
                    foreach (var y in BudgetItemList)
                    {
                        SuggBudgItems = SuggBudgItems.Concat(new [] {new SelectListItem{Text = y.SuggBudgName, Value = y.SuggBudgName, Group = CategoryGroup}});
                    }
                        
                }

                var Freq = from b in context.L_SuggestedFrequency
                           orderby b.Order
                           select new SelectListItem { Value = b.FrequencyName, Text = b.FrequencyName };

                
                myBudget.HasBudget = true;
                myBudget.Position = Budg.Position;
                myBudget.BudgetList = BudgItems;
                myBudget.Categories = Categories;
                myBudget.SuggestedBudgetItems = SuggBudgItems;
                myBudget.SuggestedFrequencies = Freq.AsEnumerable(); ;
                return myBudget;
	        }
	        catch (Exception e)
	        {
                myBudget.ErrorMessage = e.ToString();
                return myBudget;
	        }
            
        }

        public Boolean HasBudget(Guid CustomerId)
        {
            var context = new Central();
            var CustId = from b in context.Budgets
                         where b.CustomerId == CustomerId
                         select b.BudgetId;
            if (CustId == null || !CustId.Any())
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public Boolean HasBudgetItem(Guid CustomerId, string BudgetItemName)
        {
            var Context = new Central();
            var BudgetId = (from b in Context.Budgets
                            where b.CustomerId == CustomerId
                            select b.BudgetId).First();
            var BudgName = (from b in Context.BudgetItems
                            where b.BudgetId == BudgetId && b.BudgetItemName == BudgetItemName
                            select b.BudgetItemName).FirstOrDefault();
            if (BudgName != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean HasBudgetItemId(Guid BudgetId, Guid BudgetItemId)
        {
            var Context = new Central();
            var BudgItemId = (from b in Context.BudgetItems
                              where b.BudgetId == BudgetId && b.BudgetItemId == BudgetItemId
                              select b.BudgetItemId).FirstOrDefault();
            if (BudgItemId != Guid.Empty && BudgItemId != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Guid GetBudgetId(Guid CustomerId)
        {
            var Context = new Central();
            var BudgId = (from b in Context.Budgets
                         where b.CustomerId == CustomerId
                         select b.BudgetId).First();
            return BudgId;
        }

        public string CreateNewBudget(Guid CustomerId)
        {
            try
            {
                Guid NewBudgetId = Guid.NewGuid();
                var context = new Central();
                Budget myBudget = new Budget();
                myBudget.CustomerId = CustomerId;
                myBudget.BudgetId = NewBudgetId;
                myBudget.CreationDate = DateTime.Now;
                myBudget.UpdatedDate = DateTime.Now;
                context.Budgets.Add(myBudget);
                /*****
                 * Tried to see if adding Uncategorized to each was okay
                BudgetItem myFirstBudgetItem = new BudgetItem();
                myFirstBudgetItem.BudgetId = NewBudgetId;
                myFirstBudgetItem.BudgetItemAmount = Decimal.Parse("0.00");
                myFirstBudgetItem.BudgetItemCategory = "Uncategorized";
                myFirstBudgetItem.BudgetItemFrequency = "Monthly";
                myFirstBudgetItem.BudgetItemId = new Guid();
                myFirstBudgetItem.BudgetItemName = "Uncategorized";
                myFirstBudgetItem.BudgetItemType = 3;
                myFirstBudgetItem.CreationDate = DateTime.Now;
                myFirstBudgetItem.UpdateDate = DateTime.Now;
                context.BudgetItems.Add(myFirstBudgetItem);*/
                context.SaveChanges();
                string Status = "Success";
                return Status;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public string EnterNewBudgetItem(BudgetListItem ListItemToAdd, Guid CustomerId, string InputType)
        {
            try
            {
                int Type = (InputType == "SuggestedInput") ? 0 : 1;
                var Context = new Central();
                BudgetItem MyBudgetItem = new BudgetItem();
                MyBudgetItem.BudgetItemId = Guid.NewGuid();
                MyBudgetItem.BudgetId = GetBudgetId(CustomerId);
                MyBudgetItem.BudgetItemAmount = ListItemToAdd.BudgetListItemAmount;
                MyBudgetItem.BudgetItemCategory = ListItemToAdd.BudgetListItemCategory;
                MyBudgetItem.BudgetItemName = ListItemToAdd.BudgetListItemName;
                MyBudgetItem.BudgetItemFrequency = ListItemToAdd.BudgetListItemFrequency;
                MyBudgetItem.BudgetItemType = Type;
                MyBudgetItem.CreationDate = DateTime.Now;
                MyBudgetItem.UpdateDate = DateTime.Now;
                Context.BudgetItems.Add(MyBudgetItem);
                Context.SaveChanges();
                string Status = "Success";
                return Status;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public string EditBudgetItemList(List<BudgetListItem> CurrentBudgetItems, Guid CustomerId)
        {
            try
            {
                var Context = new Central();
                Guid BudgetId = GetBudgetId(CustomerId);
                foreach (var x in CurrentBudgetItems)
                {
                    Guid BudgListItemId = Guid.Parse(x.BudgetListItemId);
                    if (HasBudgetItemId(BudgetId, BudgListItemId))
                    {
                        var ListItemToUpdate = (from b in Context.BudgetItems
                                               where b.BudgetItemId == BudgListItemId && b.BudgetId == BudgetId
                                               select b).FirstOrDefault();
                        if (ListItemToUpdate.BudgetItemType == 1)
                        {
                            ListItemToUpdate.BudgetItemName = x.BudgetListItemName;
                        }
                        ListItemToUpdate.BudgetItemAmount = x.BudgetListItemAmount;
                        ListItemToUpdate.BudgetItemFrequency = x.BudgetListItemFrequency;
                        ListItemToUpdate.UpdateDate = DateTime.Now;
                        Context.Entry(ListItemToUpdate).State = System.Data.Entity.EntityState.Modified;
                    }
                }
                Context.SaveChanges();
                string Status = "Success";
                return Status;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}