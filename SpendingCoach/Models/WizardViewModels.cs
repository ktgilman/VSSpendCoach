using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SpendingCoach.Models
{
    public class WizardViewModel
    {
        public string WizardPosition { get; set; }
        public string WizardPrevPosition { get; set; }
        public WizardMap WizardPageMap { get; set; }

        public string SuccessMessage { get; set; }
        public string ErrorMessage { get; set; }

        public string PrimaryName { get; set; }
        public string SpouseName { get; set; }

        public WizardAnswers Answers { get; set; }

        public WizardHelpers Helpers { get; set; }

        public BudgetItemLists BudgetItems { get; set; }

        public UserEmployerLists UserEmployers { get; set; }

        public List<string> PossiblePositions { get; set; }

    }

    public class WizardMap
    {
        public string WizardMapPrevFirst { get; set; }
        public string WizardMapPrevSecond { get; set; }
        public string WizardMapPrevThird { get; set; }
        public string WizardMapNextFirst { get; set; }
        public string WizardMapNextSecond { get; set; }
        public string WizardMapNextThird { get; set; }
    }

    public class WizardAnswers
    {
        [DisplayName("My purpose for following a budget is:")]
        public string InitialPurpose { get; set; }

        [DisplayName("My purpose for following a budget is:")]
        public string SubsequentPurpose { get; set; }

        public Guid? MostImportantItem { get; set; }
    }

    public class WizardHelpers
    {
        public List<MostImportantBudgetItem> MostImportantBudgetItems { get; set; }
    }

    public class MostImportantBudgetItem
    {
        public Guid MostImportantItemId { get; set; }
        public string MostImportantItemName { get; set; }
    }

    public class BudgetItemLists
    {
        public List<BudgetListItem> ExistingBudgetItems { get; set; }
        public List<BudgetListItem> NewBudgetItems { get; set; }
    }

    public class UserEmployerLists
    {
        public List<UserEmployer> ExistingEmployers { get; set; }
        public List<UserEmployer> NewEmployers { get; set; }
    }

    public class UserEmployer
    {
        public Guid UserEmployerId { get; set; }
        public string UserEmployerName { get; set; }
        public string UserEmployerType { get; set; }
        public string UserEmployerUser { get; set; }
    }

    
}