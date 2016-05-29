using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SpendingCoach.Models
{
    public class BudgetViewModel
    {
        public List<BudgetListItem> BudgetList { get; set; }
        public Boolean HasBudget { get; set; }
        public string ErrorMessage { get; set; }
        public int Position { get; set; }
        public BudgetListItem NewBudgetItem { get; set; }
        public IEnumerable<SelectListItem> SuggestedBudgetItems { get; set; }
        public IEnumerable<SelectListItem> SuggestedFrequencies { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; }
        public string InputType { get; set; }
        public string SuccessMessage { get; set; }
    }

    public class BudgetListItem
    {
        public string BudgetListItemId { get; set; }

        [DisplayName("Budget Item")]
        [Required(ErrorMessage="You must choose a Budget Item")]
        public string BudgetListItemName { get; set; }

        [DisplayName("Budget Item Amount")]
        [Required(ErrorMessage="You must enter a Budget Amount")]
        [RegularExpression(@"^\d{1,5}(\.\d{1,2})?$")]
        public decimal BudgetListItemAmount { get; set; }

        [DisplayName("Budget Item Category")]
        [Required(ErrorMessage="You must choose a Budget Item Category")]
        public string BudgetListItemCategory { get; set; }

        [DisplayName("Frequency")]
        [Required(ErrorMessage="You must choose a Frequency")]
        public string BudgetListItemFrequency { get; set; }

        public int BudgetListItemType { get; set; }
    }

    public class BudgetNameToDelete
    {
        public string DeleteName { get; set; }
    }
}