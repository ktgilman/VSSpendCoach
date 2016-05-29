using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using SpendingCoach.Models;
using OFXParser;
using OfxSharpLib;
using System.IO;

namespace SpendingCoach.Controllers
{
    public class TransactionController : CentralController
    {

        public ActionResult Transaction(string message)
        {
            if (Request.IsAuthenticated)
            {
                Guid CustomerId = Guid.Parse(User.Identity.GetUserId());
                TransactionFilter TransFilterToAdd = new TransactionFilter();
                TransFilterToAdd.FilterBeginningDate = DateTime.Now.AddDays(-90);
                TransFilterToAdd.NoBeginning = false;
                TransFilterToAdd.FilterEndingDate = DateTime.Now;
                TransFilterToAdd.NoEnding = false;
                TransFilterToAdd.FilterCategory = 1;
                TransFilterToAdd.NoCriteria = false;
                TransFilterToAdd.FilterCategoryCriteria = new Guid();
                TransactionViewModel MyTransactionViewModel = CreateTransactionViewModel(CustomerId, TransFilterToAdd);
                MyTransactionViewModel.SuccessMessage = message;
                return View(MyTransactionViewModel);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
            
        }


        [HttpPost]
        public ActionResult Transaction(TransactionViewModel MyTransactionViewModel)
        {
            if (Request.IsAuthenticated)
            {
                Guid CustomerId = Guid.Parse(User.Identity.GetUserId());
                TransactionFilter TransFilterToAdd = new TransactionFilter();
                TransFilterToAdd.FilterBeginningDate = DateTime.Now.AddDays(-90);
                TransFilterToAdd.NoBeginning = false;
                TransFilterToAdd.FilterEndingDate = DateTime.Now;
                TransFilterToAdd.NoEnding = false;
                TransFilterToAdd.FilterCategory = 1;
                TransFilterToAdd.NoCriteria = false;
                TransFilterToAdd.FilterCategoryCriteria = new Guid();
                if (ModelState.IsValid)
                {
                    string Success = "";
                    string Error = "";
                    if (MyTransactionViewModel.UploadedFile != null)
                    {
                        string result = UploadTransactionsToDatabase(CustomerId, MyTransactionViewModel.UploadedFile);
                        if (result == "Success")
                        {
                            Success = "You successfully Uploaded Transactions";
                        }
                        else
                        {
                            Error = result;
                        }
                    }
                    else if (MyTransactionViewModel.TransactionToAdd != null)
                    {
                        string result = AddNewTransaction(CustomerId, MyTransactionViewModel.TransactionToAdd);
                        if (result == "Success")
                        {
                            Success = "You successfully Added a Transaction";
                        }
                        else
                        {
                            Error = result;
                        }
                    }
                    else
                    {
                        if (MyTransactionViewModel.SplitsToAdd != null)
                        {
                            string SplitResult = AddTransactionSplits(CustomerId, MyTransactionViewModel.SplitsToAdd);
                            if (SplitResult == "Success")
                            {
                                Success = "You successfully added a split. ";
                            }
                            else
                            {
                                Error = SplitResult;
                            }
                        }
                        if (MyTransactionViewModel.SplitTransactions != null)
                        {
                            string result = EditTransactionsInDatabase(CustomerId, MyTransactionViewModel.SplitTransactions);
                            if (result == "Success")
                            {
                                Success += "You successfully Edited your Split Transactions. ";
                            }
                            else if(result != "NoChange")
                            {
                                Error += result;
                            }
                        }
                        if (MyTransactionViewModel.SplitsToAdd != null || MyTransactionViewModel.SplitTransactions != null)
                        {
                            TransFilterToAdd.FilterBeginningDate = DateTime.Parse("1970-01-01");
                            TransFilterToAdd.FilterCategory = 0;
                        }
                        if (MyTransactionViewModel.Transactions != null)
                        {
                            string result = EditTransactionsInDatabase(CustomerId, MyTransactionViewModel.Transactions);
                            if (result == "Success")
                            {
                                Success += "You successfully Edited your Transactions ";
                            }
                            else if(result != "NoChange")
                            {
                                Error += result;
                            }
                        }
                    }
                    if (MyTransactionViewModel.ViewTransactionFilter != null)
                    {
                        if (MyTransactionViewModel.ViewTransactionFilter.NoBeginning == true)
                        {
                            TransFilterToAdd.NoBeginning = true;
                            TransFilterToAdd.FilterBeginningDate = DateTime.Parse("1970-01-01");
                        }
                        else
                        {
                            TransFilterToAdd.FilterBeginningDate = MyTransactionViewModel.ViewTransactionFilter.FilterBeginningDate;
                        }
                        if (MyTransactionViewModel.ViewTransactionFilter.NoEnding == true)
                        {
                            TransFilterToAdd.NoEnding = true;
                            TransFilterToAdd.FilterEndingDate = DateTime.Today;
                        }
                        else
                        {
                            TransFilterToAdd.FilterEndingDate = MyTransactionViewModel.ViewTransactionFilter.FilterEndingDate;
                        }
                        
                        if (MyTransactionViewModel.ViewTransactionFilter.NoCriteria == true)
                        {
                            TransFilterToAdd.NoCriteria = true;
                            TransFilterToAdd.FilterCategory = 0;
                        }
                        else
                        {
                            TransFilterToAdd.FilterCategory = MyTransactionViewModel.ViewTransactionFilter.FilterCategory;
                        }
                        
                        TransFilterToAdd.FilterCategoryCriteria = MyTransactionViewModel.ViewTransactionFilter.FilterCategoryCriteria;
                        TransFilterToAdd.FilterTextCriteria = MyTransactionViewModel.ViewTransactionFilter.FilterTextCriteria;
                    }
                    ModelState.Clear();
                    TransactionViewModel NewTransactionViewModel = CreateTransactionViewModel(CustomerId, TransFilterToAdd);
                    NewTransactionViewModel.ViewTransactionFilter = TransFilterToAdd;
                    NewTransactionViewModel.SuccessMessage = Success;
                    NewTransactionViewModel.ErrorMessage = Error;
                    return View(NewTransactionViewModel);
                }
                else
                {
                    MyTransactionViewModel = CreateTransactionViewModel(CustomerId, TransFilterToAdd);
                    return View(MyTransactionViewModel);
                }
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DeleteTransaction(TransactionItemToDelete TransactionToDelete)
        {
            string result = "";
            try
            {
                Guid CustomerId = Guid.Parse(User.Identity.GetUserId());
                Guid TransactionItemId = Guid.Parse(TransactionToDelete.TransactionId);
                var Context = new Central();
                var ToDelete = (from b in Context.TransactionItems
                                where b.CustomerId == CustomerId && b.TransactionItemId == TransactionItemId
                                select b).FirstOrDefault();
                Context.TransactionItems.Remove(ToDelete);
                if (TransactionToDelete.TransactionType == 3)
                {
                    DeleteSplitTransactionsForPrimary(Context, TransactionItemId);
                }
                Context.SaveChanges();
                result = "You have successfully deleted the Transaction Item - " + TransactionToDelete.TransactionName;

            }
            catch (Exception e)
            {
                result = e.ToString();
            }
            return RedirectToAction("Transaction", "Transaction", new { message = result });
        }

        public ActionResult FileToUpload()
        {
            TransactionViewModel UseTransactionViewModel = new TransactionViewModel();
            return PartialView(UseTransactionViewModel);
        }

        [HttpPost]
        public string UpdateSplitTransactionAmount(SplitTransactionAmountToEdit SplitTransEdit)
        {
            try
            {
                Guid CustomerId = Guid.Parse(User.Identity.GetUserId());
                Guid TransactionItemId = Guid.Parse(SplitTransEdit.TransactionId);
                var Context = new Central();
                var TEdit = (from b in Context.TransactionItems
                             where b.CustomerId == CustomerId && b.TransactionItemId == TransactionItemId
                             select b).FirstOrDefault();
                TEdit.TransactionItemAmount = Decimal.Parse(SplitTransEdit.TransactionAmount);
                Context.Entry(TEdit).State = System.Data.Entity.EntityState.Modified;
                Context.SaveChanges();
                return "Success";
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        [HttpPost]
        public string RemoveSplitTransaction(RemoveSplitTransaction SplitToEdit)
        {
            try
            {
                Guid CustomerId = Guid.Parse(User.Identity.GetUserId());
                Guid TransactionItemId = Guid.Parse(SplitToEdit.TransactionId);
                var Context = new Central();
                var TEdit = (from b in Context.TransactionItems
                             where b.CustomerId == CustomerId && b.TransactionItemId == TransactionItemId
                             select b).FirstOrDefault();
                TEdit.TransactionItemType = 1;
                Context.Entry(TEdit).State = System.Data.Entity.EntityState.Modified;
                Context.SaveChanges();
                return "Success";
            }
            catch(Exception e)
            {
                return e.ToString();
            }
        }

        public TransactionViewModel CreateTransactionViewModel(Guid CustomerId, TransactionFilter TransFilter)
        {
            var Context = new Central();
            List<UserTransaction> UserTransactions = new List<UserTransaction>();
            if (TransFilter.FilterCategory == 0)
            {
                UserTransactions = (from b in Context.TransactionItems
                                    join c in Context.BudgetItems
                                    on b.TransactionItemBudgetItem equals c.BudgetItemId
                                    orderby b.TransactionItemDate descending
                                    where b.CustomerId == CustomerId && b.TransactionItemType != 4 && b.TransactionItemDate >= TransFilter.FilterBeginningDate &&
                                            b.TransactionItemDate <= TransFilter.FilterEndingDate
                                    select new UserTransaction
                                    {
                                        TransactionItemId = b.TransactionItemId,
                                        TransactionAmount = b.TransactionItemAmount,
                                        TransactionDate = b.TransactionItemDate,
                                        TransactionName = b.TransactionItemName,
                                        BudgetItemId = b.TransactionItemBudgetItem,
                                        TransactionMemo = b.TransactionItemMemo,
                                        TransactionType = b.TransactionItemType,
                                        PrimaryTransactionId = b.PrimaryTransactionItem,
                                        BudgetItemDisplayName = c.BudgetItemName
                                    }).ToList();
            }
            else if(TransFilter.FilterCategory == 1)
            {
                var CategoryId = TransFilter.FilterCategoryCriteria;

                var SplitTransMatchingCategory = from b in Context.TransactionItems
                                                  where b.CustomerId == CustomerId && b.TransactionItemType == 4 && b.TransactionItemBudgetItem == CategoryId
                                                    && b.TransactionItemDate < TransFilter.FilterEndingDate && b.TransactionItemDate > TransFilter.FilterBeginningDate
                                                  select b.PrimaryTransactionItem;

                List<Guid?> MatchingSplitTransactions = new List<Guid?>();
                if(SplitTransMatchingCategory.Any()){
                    MatchingSplitTransactions = SplitTransMatchingCategory.ToList();
                }

                UserTransactions = (from b in Context.TransactionItems
                                    join c in Context.BudgetItems
                                    on b.TransactionItemBudgetItem equals c.BudgetItemId
                                        orderby b.TransactionItemDate descending
                                        where (b.CustomerId == CustomerId && b.TransactionItemType != 4  && b.TransactionItemType != 3 
                                                && b.TransactionItemDate >= TransFilter.FilterBeginningDate &&
                                                b.TransactionItemDate <= TransFilter.FilterEndingDate && b.TransactionItemBudgetItem == CategoryId) ||
                                                (MatchingSplitTransactions.Contains(b.TransactionItemId))
                                        select new UserTransaction {TransactionItemId = b.TransactionItemId, TransactionAmount = b.TransactionItemAmount, TransactionDate = b.TransactionItemDate,
                                                        TransactionName = b.TransactionItemName, BudgetItemId = b.TransactionItemBudgetItem, TransactionMemo = b.TransactionItemMemo,
                                                        TransactionType = b.TransactionItemType, PrimaryTransactionId = b.PrimaryTransactionItem, BudgetItemDisplayName = c.BudgetItemName}).ToList();
            }
            else
            {
                UserTransactions = (from b in Context.TransactionItems
                                    join c in Context.BudgetItems
                                    on b.TransactionItemBudgetItem equals c.BudgetItemId
                                        orderby b.TransactionItemDate descending
                                        where b.CustomerId == CustomerId && b.TransactionItemType != 4 && b.TransactionItemDate >= TransFilter.FilterBeginningDate &&
                                                b.TransactionItemDate <= TransFilter.FilterEndingDate
                                        select new UserTransaction
                                        {
                                            TransactionItemId = b.TransactionItemId,
                                            TransactionAmount = b.TransactionItemAmount,
                                            TransactionDate = b.TransactionItemDate,
                                            TransactionName = b.TransactionItemName,
                                            BudgetItemId = b.TransactionItemBudgetItem,
                                            TransactionMemo = b.TransactionItemMemo,
                                            TransactionType = b.TransactionItemType,
                                            PrimaryTransactionId = b.PrimaryTransactionItem,
                                            BudgetItemDisplayName = c.BudgetItemName
                                        }).ToList();
            }
            

            var UserSplitTransactions = (from b in Context.TransactionItems
                                         join c in Context.BudgetItems
                                         on b.TransactionItemBudgetItem equals c.BudgetItemId
                                          orderby b.TransactionItemDate descending
                                            where b.CustomerId == CustomerId && b.TransactionItemType == 4 && b.TransactionItemDate >= TransFilter.FilterBeginningDate &&
                                                    b.TransactionItemDate <= TransFilter.FilterEndingDate
                                             select new UserTransaction {TransactionItemId = b.TransactionItemId, TransactionAmount = b.TransactionItemAmount, TransactionDate = b.TransactionItemDate,
                                                    TransactionName = b.TransactionItemName, BudgetItemId = b.TransactionItemBudgetItem, TransactionMemo = b.TransactionItemMemo,
                                                    TransactionType = b.TransactionItemType, PrimaryTransactionId = b.PrimaryTransactionItem, BudgetItemDisplayName = c.BudgetItemName}).ToList();
            
            Guid BudgetId = GetBudgetId(CustomerId);

            var BudgListItemCategories = (from b in Context.L_Category
                                          orderby b.CategoryName
                                          select b.CategoryName).Distinct().ToList();

            var BudgListItems = (from b in Context.BudgetItems
                                         where b.BudgetId == BudgetId
                                         orderby b.BudgetItemCategory, b.BudgetItemName
                                         select b).ToList();
            IEnumerable<SelectListItem> SuggBudgItems = Enumerable.Empty<SelectListItem>().AsQueryable();
            foreach (var y in BudgListItemCategories)
            {
                SelectListGroup CategoryGroup = new SelectListGroup();
                CategoryGroup.Name = y;
                foreach (var x in BudgListItems)
                {
                    if (x.BudgetItemCategory == y)
                    {
                        SuggBudgItems = SuggBudgItems.Concat(new[] { new SelectListItem { Text = x.BudgetItemName, Value = x.BudgetItemId.ToString(), Group = CategoryGroup }});
                    }
                }
            }

            IEnumerable<SelectListItem> TransactionFilterCriteria = new[] {
                                                                         new SelectListItem {Text = "Category", Value = "1"},
                                                                         new SelectListItem {Text = "Transaction Name", Value = "2"},
                                                                         new SelectListItem {Text = "Transaction Amount", Value = "3"}};
            SelectListGroup UncategorizedGroup = new SelectListGroup();
            UncategorizedGroup.Name = "Uncategorized";
            SuggBudgItems = SuggBudgItems.Concat(new [] { new SelectListItem { Text = "Uncategorized", Value = Guid.Empty.ToString(), Group = UncategorizedGroup}});
            TransactionViewModel MyTransactionViewModel = new TransactionViewModel();
            MyTransactionViewModel.Transactions = UserTransactions;
            MyTransactionViewModel.SplitTransactions = UserSplitTransactions;
            MyTransactionViewModel.BudgetItemList = SuggBudgItems;
            MyTransactionViewModel.FilterCriteriaChoices = TransactionFilterCriteria;
            MyTransactionViewModel.ViewTransactionFilter = TransFilter;
            return MyTransactionViewModel;
        }

        public string UploadTransactionsToDatabase(Guid CustomerId, HttpPostedFileBase UploadedFile)
        {
            try
            {
                var Context = new Central();
                var StreamToUpdate = UploadedFile.InputStream;
                var parser = new OfxDocumentParser();
                string StreamString = StreamToString(StreamToUpdate);
                var ofxDocument = parser.Import(StreamString);
                foreach (var x in ofxDocument.Transactions)
                {
                    if (!IsExistingTransaction(CustomerId, x))
                    {
                        TransactionItem UserTransaction = new TransactionItem();
                        UserTransaction.TransactionItemId = Guid.NewGuid();
                        UserTransaction.CustomerId = CustomerId;
                        UserTransaction.TransactionItemName = x.Name;
                        UserTransaction.TransactionItemDate = DateTime.Parse(x.Date.ToShortDateString());
                        UserTransaction.TransactionItemAmount = x.Amount;
                        UserTransaction.TransactionItemBudgetItem = Guid.Empty;
                        UserTransaction.TransactionItemType = 1;
                        UserTransaction.CreationDate = DateTime.Now;
                        UserTransaction.UpdateDate = DateTime.Now;
                        Context.TransactionItems.Add(UserTransaction);
                    }
                }
                Context.SaveChanges();
                string Status = "Success";
                return Status;
            }
            catch(Exception e)
            {
                return e.ToString();
            }
        }

        public string AddNewTransaction(Guid CustomerId, UserTransaction TransactionToAdd)
        {
            try
            {
                var Context = new Central();
                TransactionItem DBTrans = new TransactionItem();
                DBTrans.TransactionItemId = Guid.NewGuid();
                DBTrans.TransactionItemDate = TransactionToAdd.TransactionDate;
                DBTrans.TransactionItemName = TransactionToAdd.TransactionName;
                DBTrans.TransactionItemAmount = TransactionToAdd.TransactionAmount;
                DBTrans.TransactionItemMemo = TransactionToAdd.TransactionMemo;
                DBTrans.TransactionItemBudgetItem = TransactionToAdd.BudgetItemId;
                DBTrans.TransactionItemType = 2;
                DBTrans.CustomerId = CustomerId;
                DBTrans.CreationDate = DateTime.Now;
                DBTrans.UpdateDate = DateTime.Now;
                Context.TransactionItems.Add(DBTrans);
                Context.SaveChanges();
                string status = "Success";
                return status;
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }

        public string StreamToString(Stream TargetStream)
        {
            using (var reader = new StreamReader(TargetStream))
            {
                string Stream = reader.ReadToEnd();
                int Split = Stream.IndexOf("<OFX>");
                string Header = Stream.Substring(0, Split);
                Header = RemoveAllSpaces(Header);
                string Body = Stream.Substring(Split);
                string FullReturnString = Header + Body;
                return FullReturnString;
            }
        }

        public string RemoveAllSpaces(string EditString)
        {
            Boolean check = true;
            List<string> StringArray = new List<string>();
            while (check)
            {
                int Instance = EditString.IndexOf(":");
                int NextInstance = Instance + 1;
                if (Instance == -1)
                {
                    StringArray.Add(EditString);
                    check = false;
                }
                else
                {
                    var SpaceAddition = 0;
                    var Test = EditString.Substring(NextInstance);
                    if (EditString.Substring(NextInstance, 1) == " ")
                    {
                        SpaceAddition = 1;
                    }
                    StringArray.Add(EditString.Substring(0, NextInstance));
                    EditString = EditString.Substring(NextInstance + SpaceAddition);
                }
            }
            string ReturnString = "";
            foreach (var x in StringArray)
            {
                ReturnString = ReturnString + x;
            }
            return ReturnString;
        }

        public string EditTransactionsInDatabase(Guid CustomerId, List<UserTransaction> TransactionItems)
        {
            string result = "";
            try
            {
                var Context = new Central();
                Boolean Modified = false;
                Boolean ChangeExists = false;
                foreach (var x in TransactionItems)
                {
                    Modified = false;
                    var TItem = (from b in Context.TransactionItems
                                 where b.TransactionItemId == x.TransactionItemId && b.CustomerId == CustomerId
                                 select b).FirstOrDefault();
                    if (TItem != null)
                    {
                        if (TItem.TransactionItemType == 2 && x.TransactionType != null)
                        {
                            if (TItem.TransactionItemDate != x.TransactionDate)
                            {
                                TItem.TransactionItemDate = x.TransactionDate;
                                Modified = true;
                            }
                            if (TItem.TransactionItemName != x.TransactionName)
                            {
                                TItem.TransactionItemName = x.TransactionName;
                                Modified = true;
                            }
                            if (TItem.TransactionItemAmount != x.TransactionAmount)
                            {
                                TItem.TransactionItemAmount = x.TransactionAmount;
                                Modified = true;
                            }
                        }
                        if (TItem.TransactionItemType == 4 && x.TransactionType != null && x.TransactionAmount != 0)
                        {
                            if (TItem.TransactionItemAmount != x.TransactionAmount)
                            {
                                TItem.TransactionItemAmount = x.TransactionAmount;
                                Modified = true;
                            }

                        }
                        if (TItem.TransactionItemType != 3 && x.TransactionType != null && x.TransactionAmount != 0)
                        {
                            if (TItem.TransactionItemBudgetItem != x.BudgetItemId)
                            {
                                TItem.TransactionItemBudgetItem = x.BudgetItemId;
                                Modified = true;
                            }
                            if (TItem.TransactionItemMemo != x.TransactionMemo)
                            {
                                TItem.TransactionItemMemo = x.TransactionMemo;
                                Modified = true;

                            }
                        }
                        
                        if (Modified)
                        {
                            TItem.UpdateDate = DateTime.Now;
                            Context.Entry(TItem).State = System.Data.Entity.EntityState.Modified;
                            ChangeExists = true;
                        }
                    }
                    
                }
                if (ChangeExists)
                {
                    Context.SaveChanges();
                    result = "Success";
                }
                else
                {
                    result = "NoChange";
                }
                
            }
            catch(Exception e)
            {
                result = e.ToString();
            }
            return result;
        }

        public string AddTransactionSplits(Guid CustomerId, List<UserTransaction> SplitsToAdd)
        {
            try
            {
                var Context = new Central();
                foreach (var x in SplitsToAdd)
                {
                    if (x.TransactionAmount != 0)
                    {
                        TransactionItem NewSplitTransaction = new TransactionItem();
                        NewSplitTransaction.CustomerId = CustomerId;
                        NewSplitTransaction.TransactionItemId = Guid.NewGuid();
                        NewSplitTransaction.TransactionItemDate = x.TransactionDate;
                        NewSplitTransaction.TransactionItemName = x.TransactionName;
                        NewSplitTransaction.TransactionItemAmount = x.TransactionAmount;
                        NewSplitTransaction.TransactionItemMemo = x.TransactionMemo;
                        NewSplitTransaction.PrimaryTransactionItem = x.PrimaryTransactionId;
                        NewSplitTransaction.TransactionItemBudgetItem = x.BudgetItemId;
                        NewSplitTransaction.CreationDate = DateTime.Now;
                        NewSplitTransaction.UpdateDate = DateTime.Now;
                        NewSplitTransaction.TransactionItemType = 4;
                        Context.TransactionItems.Add(NewSplitTransaction);
                        var PrimaryTransactionToChange = (from b in Context.TransactionItems
                                                          where b.TransactionItemId == x.PrimaryTransactionId && b.CustomerId == CustomerId
                                                          select b).FirstOrDefault();
                        PrimaryTransactionToChange.TransactionItemType = 3;
                        PrimaryTransactionToChange.UpdateDate = DateTime.Now;
                        Context.Entry(PrimaryTransactionToChange).State = System.Data.Entity.EntityState.Modified;
                    }
                }
                Context.SaveChanges();
                return "Success";
            }
            catch(Exception e)
            {
                return e.ToString();
            }
        }

        public Boolean IsExistingTransaction(Guid CustomerId, Transaction TransactionToUpload)
        {
            var Context = new Central();
            var ExistingTransaction = (from b in Context.TransactionItems
                                       where b.CustomerId == CustomerId && b.TransactionItemName == TransactionToUpload.Name &&
                                                 b.TransactionItemDate == TransactionToUpload.Date && b.TransactionItemAmount == TransactionToUpload.Amount
                                       select b).FirstOrDefault();
            if (ExistingTransaction == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public Boolean DeleteSplitTransactionsForPrimary(Central Context, Guid PrimaryTransactionId)
        {
            try
            {
                var SplitTransactionsToDelete = from b in Context.TransactionItems
                                                where b.PrimaryTransactionItem == PrimaryTransactionId
                                                select b;
                foreach(var x in SplitTransactionsToDelete){
                    Context.TransactionItems.Remove(x);
                }
                return true;
            }
            catch
            {
                return false;
            }
            
            
        }
    }
}