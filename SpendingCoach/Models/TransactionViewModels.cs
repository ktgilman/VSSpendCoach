using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SpendingCoach.Models
{
    public class TransactionViewModel
    {
        [DisplayName("Upload Transactions")]
        public HttpPostedFileBase UploadedFile { get; set; }

        public List<UserTransaction> Transactions { get; set; }

        public List<UserTransaction> SplitTransactions { get; set; }

        public IEnumerable<SelectListItem> BudgetItemList { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }
        public UserTransaction TransactionToAdd { get; set; }
        public List<UserTransaction> SplitsToAdd { get; set; }

        public TransactionFilter ViewTransactionFilter { get; set; }
        public IEnumerable<SelectListItem> FilterCriteriaChoices { get; set; }

    }

    public class UserTransaction
    {
        public Guid TransactionItemId { get; set; }

        [DisplayName("Vendor Name")]
        public string TransactionName { get; set; }

        [DisplayName("Amount")]
        public decimal TransactionAmount { get; set; }

        [DisplayName("Transaction Date")]
        public DateTime TransactionDate { get; set; }

        [DisplayName("Budget Item")]
        public Nullable<Guid> BudgetItemId { get; set; }

        [DisplayName("Memo")]
        public string TransactionMemo { get; set; }

        public Nullable<int> TransactionType { get; set; }

        public Nullable<Guid> PrimaryTransactionId { get; set; }

        public string BudgetItemDisplayName { get; set; }
    }

    public class TransactionItemToDelete
    {
        public string TransactionId { get; set; }

        public string TransactionName { get; set; }

        public int TransactionType { get; set; }
    }

    public class SplitTransactionAmountToEdit
    {
        public string TransactionId { get; set; }

        public string TransactionAmount { get; set; }
    }

    public class RemoveSplitTransaction
    {
        public string TransactionId { get; set; }
    }

    public class TransactionFilter
    {
        public DateTime FilterBeginningDate { get; set; }
        public DateTime FilterEndingDate { get; set; }
        public int FilterCategory { get; set; }
        public string FilterTextCriteria { get; set; }
        public Guid FilterCategoryCriteria { get; set; }

        [DisplayName("No Beginning Date")]
        public Boolean NoBeginning { get; set; }

        [DisplayName("No Ending Date")]
        public Boolean NoEnding { get; set; }

        [DisplayName("No Criteria")]
        public Boolean NoCriteria { get; set; }
    }
}