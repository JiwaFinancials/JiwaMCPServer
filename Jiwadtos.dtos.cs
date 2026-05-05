using JiwaFinancials.Jiwa.JiwaServiceModel.CustomFields;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors.Classification;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors.Category;
using JiwaFinancials.Jiwa.JiwaServiceModel.Debtors.PricingGroup;
using JiwaFinancials.Jiwa.JiwaServiceModel.DebtorSystemTemplates;
using JiwaFinancials.Jiwa.JiwaServiceModel.GeneralLedger;
using JiwaFinancials.Jiwa.JiwaServiceModel.Notes;
using JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrders;
using JiwaFinancials.Jiwa.JiwaServiceModel.Staff;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tags;
using JiwaFinancials.Jiwa.JiwaServiceModel.Tax;
using JiwaFinancials.Jiwa.JiwaServiceModel.Inventory.Classification;
using JiwaFinancials.Jiwa.JiwaServiceModel.Inventory.Category;
using JiwaFinancials.Jiwa.JiwaServiceModel.Inventory;
using ServiceStack;
using ServiceStack.DataAnnotations;
using System.Reflection.Metadata;
using System.Runtime.Serialization;

#region "Standard Jiwa API DTOs"
#region "Request DTOs"
namespace JiwaFinancials.Jiwa.JiwaServiceModel
{
    #region "Debtors"
    [Route("/Debtors/{DebtorID}", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    [ApiResponse(Description = "No debtor with the DebtorID provided was found", StatusCode = 404)]
    public partial class DebtorGETRequest
        : IReturn<Debtor>
    {
        public virtual string? DebtorID { get; set; }
    }
    #endregion

    #region "Inventory"
    [Route("/Inventory/{InventoryID}", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    [ApiResponse(Description = "No inventory with the InventoryID provided was found", StatusCode = 404)]
    public partial class InventoryGETRequest
        : IReturn<InventoryItem>
    {
        public virtual string? InventoryID { get; set; }
    }
    #endregion

    #region "Sales Orders"
    [Route("/SalesOrders/{InvoiceID}", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    [ApiResponse(Description = "No Sales Order with the InvoiceID provided was found", StatusCode = 404)]
    public partial class SalesOrderGETRequest
        : IReturn<JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrders.SalesOrder>
    {
        public virtual string? InvoiceID { get; set; }
    }

    [Route("/SalesOrders", "POST")]
    [ApiResponse(Description = "Created OK", StatusCode = 201)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    public partial class SalesOrderPOSTRequest
            : SalesOrder, IReturn<SalesOrder>
    {
        [IgnoreDataMember]
        public new virtual string? InvoiceID { get; set; }

        [IgnoreDataMember]
        public new virtual SalesOrderStatuses? Status { get; set; }

        [IgnoreDataMember]
        public new virtual DateTimeOffset? LastSavedDateTime { get; set; }

        [IgnoreDataMember]
        public new virtual DateTime? DeliveredDate { get; set; }

        [IgnoreDataMember]
        public new virtual bool? Delivered { get; set; }

        [IgnoreDataMember]
        public new virtual DateTime? RCTIDate { get; set; }

        [IgnoreDataMember]
        public new virtual string? StaffTitle { get; set; }

        [IgnoreDataMember]
        public new virtual string? DebtorName { get; set; }

        [IgnoreDataMember]
        public new virtual string? DebtorEmailAddress { get; set; }
    }

    [Route("/SalesOrders/{InvoiceID}", "PATCH")]
    [ApiResponse(Description = "Updated OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    [ApiResponse(Description = "No Sales Order with the InvoiceID provided was found", StatusCode = 404)]
    public partial class SalesOrderPATCHRequest
            : SalesOrder, IReturn<SalesOrder>
    {
        [IgnoreDataMember]
        public new virtual string? Type { get; set; }

        [IgnoreDataMember]
        public new virtual string? DebtorID { get; set; }

        [IgnoreDataMember]
        public new virtual string? DebtorAccountNo { get; set; }

        [IgnoreDataMember]
        public new virtual DateTimeOffset? LastSavedDateTime { get; set; }

        [IgnoreDataMember]
        public new virtual DateTime? DeliveredDate { get; set; }

        [IgnoreDataMember]
        public new virtual bool? Delivered { get; set; }

        [IgnoreDataMember]
        public new virtual DateTime? RCTIDate { get; set; }

        [IgnoreDataMember]
        public new virtual string? StaffTitle { get; set; }

        [IgnoreDataMember]
        public new virtual string? DebtorName { get; set; }

        [IgnoreDataMember]
        public new virtual string? DebtorEmailAddress { get; set; }

        [IgnoreDataMember]
        public new virtual string? LogicalID { get; set; }

        [IgnoreDataMember]
        public new virtual string? LogicalWarehouseDescription { get; set; }

        [IgnoreDataMember]
        public new virtual string? PhysicalWarehouseDescription { get; set; }

        public new virtual string? InvoiceID { get; set; }
    }

    [Route("/SalesOrders/{InvoiceID}/Historys/{InvoiceHistoryID}/Lines", "POST")]
    [ApiResponse(Description = "Created OK", StatusCode = 201)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    [ApiResponse(Description = "No Sales Order Line with the InvoiceID or InvoiceLineID provided was found", StatusCode = 404)]
    public partial class SalesOrderLinePOSTRequest
        : SalesOrderLine, IReturn<SalesOrderLine>
    {
        [IgnoreDataMember]
        public new virtual string? InvoiceLineID { get; set; }

        [IgnoreDataMember]
        public new virtual decimal? PriceExGst { get; set; }

        [IgnoreDataMember]
        public new virtual decimal? TaxToCharge { get; set; }

        [IgnoreDataMember]
        public new virtual decimal? UnitCost { get; set; }

        [IgnoreDataMember]
        public new virtual bool? FixPrice { get; set; }

        [IgnoreDataMember]
        public new virtual decimal? LineTotal { get; set; }

        [IgnoreDataMember]
        public new virtual decimal? Weight { get; set; }

        [IgnoreDataMember]
        public new virtual decimal? Cubic { get; set; }

        [IgnoreDataMember]
        public new virtual decimal? QuotedDiscountedPrice { get; set; }

        [IgnoreDataMember]
        public new virtual decimal? QuotedDiscountPercentage { get; set; }

        [IgnoreDataMember]
        public new virtual short? QuantityDecimalPlaces { get; set; }

        [IgnoreDataMember]
        public new virtual decimal? QuantityOriginalOrdered { get; set; }

        [IgnoreDataMember]
        public new virtual bool? NonInventory { get; set; }

        [IgnoreDataMember]
        public new virtual string? CostCenter { get; set; }

        [IgnoreDataMember]
        public new virtual string? Stage { get; set; }

        [IgnoreDataMember]
        public new virtual SalesOrderKitLineTypesEnum? KitLineType { get; set; }

        [IgnoreDataMember]
        public new virtual decimal? KitUnits { get; set; }

        [IgnoreDataMember]
        public new virtual string? KitHeaderLineID { get; set; }

        public virtual string? InvoiceID { get; set; }
        public virtual string? InvoiceHistoryID { get; set; }
    }
    #endregion
}
#endregion

#region "Models"
#region "Custom Fields"
namespace JiwaFinancials.Jiwa.JiwaServiceModel.CustomFields
{
    public enum CellTypes
    {
        Date = 0,
        Text = 1,
        Float = 2,
        Integer = 3,
        Lookup = 7,
        Combo = 8,
        Checkbox = 10,
    }

    public partial class CustomField
    {
        public virtual string? SettingID { get; set; }
        public virtual string? SettingName { get; set; }
        public virtual string? PluginID { get; set; }
        public virtual string? PluginName { get; set; }
        public virtual CellTypes CellType { get; set; }
        public virtual int DisplayOrder { get; set; }
    }

    public partial class CustomFieldValue
    {
        public virtual string? SettingID { get; set; }
        public virtual string? SettingName { get; set; }
        public virtual string? Contents { get; set; }
        public virtual string? PluginID { get; set; }
        public virtual string? PluginName { get; set; }
    }

}
#endregion

#region "Debtors"
namespace JiwaFinancials.Jiwa.JiwaServiceModel.Debtors
{
    public partial class Debtor
    {
        public Debtor()
        {
            ContactNames = new List<DebtorContactName> { };
            GroupMemberships = new List<DebtorGroupMembership> { };
            BranchDebtors = new List<DebtorBranchDebtor> { };
            DeliveryAddresses = new List<DebtorDeliveryAddress> { };
            FreightForwarderAddresses = new List<DebtorFreightForwarderAddress> { };
            Notes = new List<Note> { };
            CreditNotes = new List<Note> { };
            Directors = new List<DebtorDirector> { };
            Budgets = new List<DebtorBudget> { };
            DebtorPartNumbers = new List<DebtorPartNumber> { };
            CustomFieldValues = new List<CustomFieldValue> { };
            Documents = new List<Document> { };
            DebtorSystems = new List<DebtorSystem> { };
            DebtorLedgers = new List<DebtorLedger> { };
            TagMemberships = new List<Tag> { };
            Balances = new List<DebtorBalance> { };
        }

        public virtual decimal? CreditLimit { get; set; }
        public virtual int? EarlyPaymentDiscountDays { get; set; }
        public virtual decimal? EarlyPaymentDiscountAmount { get; set; }
        public virtual DateTime? LastPurchaseDate { get; set; }
        public virtual DateTime? LastPaymentDate { get; set; }
        public virtual decimal? StandingDiscountOnInvoices { get; set; }
        public virtual bool? AccountOnHold { get; set; }
        public virtual decimal? CurrentBalance { get; set; }
        public virtual decimal? Period1Balance { get; set; }
        public virtual decimal? Period2Balance { get; set; }
        public virtual decimal? Period3Balance { get; set; }
        public virtual decimal? Period4Balance { get; set; }
        public virtual bool? NotifyRequired { get; set; }
        public virtual bool? WebAccess { get; set; }
        public virtual DateTime? CommenceDate { get; set; }
        public virtual TradingStatuses? TradingStatus { get; set; }
        public virtual PeriodTypes? PeriodType { get; set; }
        public virtual bool? IsCashOnly { get; set; }
        public virtual int? TermsDays { get; set; }
        public virtual TermsTypes? TermsType { get; set; }
        public virtual bool? ExcludeFromAging { get; set; }
        public virtual bool? DebtorIsBranchAccount { get; set; }
        public virtual decimal? RemainingNormalPrepaidLabourPackHours { get; set; }
        public virtual decimal? RemainingSpecialPrepaidLabourPackHours { get; set; }
        public virtual short? FXDecimalPlaces { get; set; }
        public virtual string? DebtorID { get; set; }
        public virtual string? ProspectID { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual string? AccountNo { get; set; }
        public virtual string? AltAccountNo { get; set; }
        public virtual string? Name { get; set; }
        public virtual string? Address1 { get; set; }
        public virtual string? Address2 { get; set; }
        public virtual string? Address3 { get; set; }
        public virtual string? Address4 { get; set; }
        public virtual string? Postcode { get; set; }
        public virtual string? Country { get; set; }
        public virtual string? Phone { get; set; }
        public virtual string? Fax { get; set; }
        public virtual string? EmailAddress { get; set; }
        public virtual string? ACN { get; set; }
        public virtual string? ABN { get; set; }
        public virtual string? AustPostDPID { get; set; }
        public virtual string? AustPostBCSP { get; set; }
        public virtual string? BankName { get; set; }
        public virtual string? BankAccountNo { get; set; }
        public virtual string? BankBSBN { get; set; }
        public virtual string? BankAccountName { get; set; }
        public virtual string? TaxExemptionNo { get; set; }
        public virtual string? NotifyAddress { get; set; }
        public virtual string? ParentDebtorID { get; set; }
        public virtual string? ParentDebtorAccountNo { get; set; }
        public virtual string? ParentDebtorName { get; set; }
        public virtual string? PriceSchemeID { get; set; }
        public virtual string? PriceSchemeDescription { get; set; }
        public virtual string? TradingName { get; set; }
        public virtual string? CompanyName { get; set; }
        public virtual string? ProprietorsName { get; set; }
        public virtual string? FaxHeader { get; set; }
        public virtual string? DefaultCurrencyID { get; set; }
        public virtual string? DefaultCurrencyName { get; set; }
        public virtual string? DefaultCurrencyShortName { get; set; }
        public virtual short? DefaultCurrencyDecimalPlaces { get; set; }
        public virtual string? BPayReference { get; set; }
        public virtual DebtorClassification? Classification { get; set; }
        public virtual DebtorCategory? Category1 { get; set; }
        public virtual DebtorCategory? Category2 { get; set; }
        public virtual DebtorCategory? Category3 { get; set; }
        public virtual DebtorCategory? Category4 { get; set; }
        public virtual DebtorCategory? Category5 { get; set; }
        public virtual DebtorPricingGroup? PricingGroup { get; set; }
        public virtual List<DebtorContactName> ContactNames { get; set; }
        public virtual List<DebtorGroupMembership> GroupMemberships { get; set; }
        public virtual List<DebtorBranchDebtor> BranchDebtors { get; set; }
        public virtual List<DebtorDeliveryAddress> DeliveryAddresses { get; set; }
        public virtual List<DebtorFreightForwarderAddress> FreightForwarderAddresses { get; set; }
        public virtual List<Note> Notes { get; set; }
        public virtual List<Note> CreditNotes { get; set; }
        public virtual List<DebtorDirector> Directors { get; set; }
        public virtual List<DebtorBudget> Budgets { get; set; }
        public virtual List<DebtorPartNumber> DebtorPartNumbers { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
        public virtual List<Document> Documents { get; set; }
        public virtual List<DebtorSystem> DebtorSystems { get; set; }
        public virtual List<DebtorLedger> DebtorLedgers { get; set; }
        public virtual List<Tag> TagMemberships { get; set; }
        public virtual List<DebtorBalance> Balances { get; set; }
        public enum TradingStatuses
        {
            e_DebtorTradingStatusInActive,
            e_DebtorTradingStatusActive,
        }

        public enum PeriodTypes
        {
            Weekly,
            Fortnightly,
            Monthly,
            Custom,
        }

        public enum TermsTypes
        {
            Invoice,
            Statement,
        }

    }

    public partial class DebtorBalance
    {
        public virtual string? CurrencyID { get; set; }
        public virtual string? CurrencyName { get; set; }
        public virtual string? CurrencyShortName { get; set; }
        public virtual short? CurrencyDecimalPlaces { get; set; }
        public virtual decimal? Period1 { get; set; }
        public virtual decimal? Period2 { get; set; }
        public virtual decimal? Period3 { get; set; }
        public virtual decimal? Period4 { get; set; }
        public virtual decimal? Total { get; set; }
        public virtual decimal? FXPeriod1 { get; set; }
        public virtual decimal? FXPeriod2 { get; set; }
        public virtual decimal? FXPeriod3 { get; set; }
        public virtual decimal? FXPeriod4 { get; set; }
        public virtual decimal? FXTotal { get; set; }
    }

    public partial class DebtorBranchDebtor
    {
        public virtual string? DebtorID { get; set; }
        public virtual string? AccountNo { get; set; }
        public virtual string? Name { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
    }

    public partial class DebtorBudget
    {
        public virtual string? BudgetID { get; set; }
        public virtual Period? Period { get; set; }
        public virtual decimal? LastBudget { get; set; }
        public virtual decimal? CurrentBudget { get; set; }
        public virtual decimal? NextBudget { get; set; }
    }

    public partial class DebtorContactName
    {
        public DebtorContactName()
        {
            TagMemberships = new List<Tag> { };
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual bool? DefaultContact { get; set; }
        public virtual bool? DebtorContact { get; set; }
        public virtual bool? CreditorContact { get; set; }
        public virtual string? ContactNameID { get; set; }
        public virtual string? ContactID { get; set; }
        public virtual string? AccountNo { get; set; }
        public virtual string? Title { get; set; }
        public virtual string? FirstName { get; set; }
        public virtual string? Surname { get; set; }
        public virtual string? PrimaryPositionID { get; set; }
        public virtual string? PrimaryPositionName { get; set; }
        public virtual string? SecondaryPositionID { get; set; }
        public virtual string? SecondaryPositionName { get; set; }
        public virtual string? TertiaryPositionID { get; set; }
        public virtual string? TertiaryPositionName { get; set; }
        public virtual string? Phone { get; set; }
        public virtual string? Mobile { get; set; }
        public virtual string? Fax { get; set; }
        public virtual string? EmailAddress { get; set; }
        public virtual string? ProspectID { get; set; }
        public virtual string? LogonCode { get; set; }
        public virtual string? LogonPassword { get; set; }
        public virtual string? ExternalAppRecID { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual bool? LogonCodeChangedByUser { get; set; }
        public virtual string? CurrentCustomerWebPortalPassword { get; set; }
        public virtual string? NewCustomerWebPortalPassword { get; set; }
        public virtual List<Tag> TagMemberships { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
    }

    public partial class DebtorContactNameTag
        : Tag
    {
        public DebtorContactNameTag()
        {
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
    }

    public partial class DebtorDeliveryAddress
    {
        public DebtorDeliveryAddress()
        {
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual bool? IsDefault { get; set; }
        public virtual string? DeliveryAddressID { get; set; }
        public virtual string? DeliveryAddressName { get; set; }
        public virtual string? DeliveryAddressCode { get; set; }
        public virtual string? Address1 { get; set; }
        public virtual string? Address2 { get; set; }
        public virtual string? Address3 { get; set; }
        public virtual string? Address4 { get; set; }
        public virtual string? Postcode { get; set; }
        public virtual string? Country { get; set; }
        public virtual string? Notes { get; set; }
        public virtual string? CourierDetails { get; set; }
        public virtual string? EDIStoreLocationCode { get; set; }
        public virtual decimal? Latitude { get; set; }
        public virtual decimal? Longitude { get; set; }
        public virtual string? EmailAddress { get; set; }
        public virtual string? Phone { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
    }

    public partial class DebtorDirector
    {
        public virtual string? DirectorID { get; set; }
        public virtual string? Name { get; set; }
        public virtual string? Address { get; set; }
        public virtual string? OfficeHeld { get; set; }
    }

    public partial class DebtorFreightForwarderAddress
    {
        public virtual bool? IsDefault { get; set; }
        public virtual string? FreightForwarderAddressID { get; set; }
        public virtual string? Address1 { get; set; }
        public virtual string? Address2 { get; set; }
        public virtual string? Address3 { get; set; }
        public virtual string? Address4 { get; set; }
        public virtual string? Country { get; set; }
        public virtual string? Notes { get; set; }
        public virtual string? Postcode { get; set; }
        public virtual string? EmailAddress { get; set; }
        public virtual string? Phone { get; set; }
        public virtual string? CourierDetails { get; set; }
        public virtual decimal? Latitude { get; set; }
        public virtual decimal? Longitude { get; set; }
    }

    public partial class DebtorGroupMembership
    {
        public DebtorGroupMembership()
        {
            RowVersion = new byte[] { };
        }

        public virtual bool? IsDefault { get; set; }
        public virtual string? GroupMembershipID { get; set; }
        public virtual string? GroupRecID { get; set; }
        public virtual string? GroupDescription { get; set; }
        public virtual string? StaffID { get; set; }
        public virtual string? StaffUsername { get; set; }
        public virtual string? StaffTitle { get; set; }
        public virtual string? StaffFirstName { get; set; }
        public virtual string? StaffSurname { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual byte[] RowVersion { get; set; }
    }

    public partial class DebtorLedger
    {
        public virtual string? LedgerID { get; set; }
        public virtual string? Name { get; set; }
        public virtual string? LedgerAccountID { get; set; }
        public virtual string? LedgerAccountNo { get; set; }
        public virtual string? LedgerAccountDescription { get; set; }
    }

    public partial class DebtorPartNumber
    {
        public virtual string? PartNumberID { get; set; }
        public virtual string? InventoryID { get; set; }
        public virtual string? PartNo { get; set; }
        public virtual string? DebtorPartNo { get; set; }
        public virtual string? DebtorBarcode { get; set; }
    }

    public partial class DebtorSystem
    {
        public DebtorSystem()
        {
            Fields = new List<DebtorSystemField> { };
        }

        public virtual DebtorSystemTemplate? Template { get; set; }
        public virtual string? SystemID { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual string? Description { get; set; }
        public virtual List<DebtorSystemField> Fields { get; set; }
    }

    public partial class DebtorSystemField
    {
        public virtual DebtorSystemTemplateField? TemplateField { get; set; }
        public virtual string? SystemFieldID { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual string? Contents { get; set; }
    }

    public partial class DebtorTag
        : Tag
    {
        public DebtorTag()
        {
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
    }

}

namespace JiwaFinancials.Jiwa.JiwaServiceModel.Debtors.Classification
{
    public partial class DebtorClassification
    {
        public DebtorClassification()
        {
            DebtorLedgers = new List<DebtorLedger> { };
            CustomFields = new List<CustomFieldValue> { };
        }

        public virtual string? ClassificationID { get; set; }
        public virtual string? Description { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual bool? IsDefault { get; set; }
        public virtual int? TermsDays { get; set; }
        public virtual DebtorTermsTypes? TermsType { get; set; }
        public virtual string? PricingGroupID { get; set; }
        public virtual string? PricingGroupDescription { get; set; }
        public virtual string? SellPricingSchemeID { get; set; }
        public virtual string? SellPricingSchemeDescription { get; set; }
        public virtual List<DebtorLedger> DebtorLedgers { get; set; }
        public virtual List<CustomFieldValue> CustomFields { get; set; }
        public enum DebtorTermsTypes
        {
            Invoice,
            Statement,
        }

    }
}

namespace JiwaFinancials.Jiwa.JiwaServiceModel.Debtors.Category
{
    public partial class DebtorCategory
    {
        public virtual string? CategoryID { get; set; }
        public virtual string? Description { get; set; }
        public virtual int? CategoryNo { get; set; }
        public virtual bool? IsDefault { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
    }

}

namespace JiwaFinancials.Jiwa.JiwaServiceModel.Debtors.PricingGroup
{
    public partial class DebtorPricingGroup
    {
        public virtual string? PricingGroupID { get; set; }
        public virtual string? Description { get; set; }
        public virtual bool? IsDefault { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
    }

}

namespace JiwaFinancials.Jiwa.JiwaServiceModel.DebtorSystemTemplates
{
    public partial class DebtorSystemTemplate
    {
        public DebtorSystemTemplate()
        {
            RowHash = new byte[] { };
            Fields = new List<DebtorSystemTemplateField> { };
            References = new List<DebtorSystemTemplateReference> { };
        }

        public virtual string? DebtorSystemTemplateID { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual bool? IsEnabled { get; set; }
        public virtual string? Name { get; set; }
        public virtual string? Code { get; set; }
        public virtual byte[] RowHash { get; set; }
        public virtual List<DebtorSystemTemplateField> Fields { get; set; }
        public virtual List<DebtorSystemTemplateReference> References { get; set; }
    }

    public partial class DebtorSystemTemplateField
    {
        public DebtorSystemTemplateField()
        {
            RowHash = new byte[] { };
        }

        public virtual string? DebtorSystemTemplateFieldID { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual string? Name { get; set; }
        public virtual DebtorSystemTemplateFieldType? FieldType { get; set; }
        public virtual string? ComboText { get; set; }
        public virtual string? DefaultValue { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual byte[] RowHash { get; set; }
        public enum DebtorSystemTemplateFieldType
        {
            Date = 0,
            Text = 1,
            Float = 2,
            Integer = 3,
            Lookup = 7,
            Combo = 8,
            Checkbox = 10,
        }

    }

    public partial class DebtorSystemTemplateReference
    {
        public DebtorSystemTemplateReference()
        {
            RowHash = new byte[] { };
        }

        public virtual string? DebtorSystemTemplateReferenceID { get; set; }
        public virtual string? AssemblyFullName { get; set; }
        public virtual string? AssemblyName { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual byte[] RowHash { get; set; }
    }

}
#endregion

#region "General Ledger"
namespace JiwaFinancials.Jiwa.JiwaServiceModel.GeneralLedger
{
    public partial class Account
    {
        public virtual string? LedgerID { get; set; }
        public virtual string? AccountNo { get; set; }
        public virtual string? Description { get; set; }
    }

    public partial class Period
    {
        public virtual DateTime? StartDate { get; set; }
        public virtual DateTime? EndDate { get; set; }
        public virtual int? FinancialYearPeriodNo { get; set; }
        public virtual string? PeriodID { get; set; }
    }

}

#endregion

#region "Inventory"
namespace JiwaFinancials.Jiwa.JiwaServiceModel.Inventory
{
    public partial class InventoryItem
    {
        public InventoryItem()
        {
            Picture = new byte[] { };
            InventoryLedgers = new List<InventoryLedger> { };
            Notes = new List<Note> { };
            Documents = new List<Document> { };
            CustomFieldValues = new List<CustomFieldValue> { };
            Regions = new List<InventoryRegion> { };
            DebtorPrices = new List<InventoryDebtorPrice> { };
            DebtorClassPrices = new List<InventoryDebtorClassificationPrice> { };
            DebtorPriceGroupInventorySpecificPrices = new List<InventoryDebtorPriceGroupInventorySpecific> { };
            AlternateChildren = new List<InventoryAlternateChild> { };
            AlternateParents = new List<InventoryAlternateParent> { };
            Components = new List<InventoryComponent> { };
            WarehouseSOHs = new List<InventoryWarehouseSOH> { };
            DebtorPartNumbers = new List<InventoryDebtorPartNumber> { };
            GroupMemberships = new List<InventoryGroupMembership> { };
            OtherDescriptions = new List<InventoryOtherDescription> { };
            OrderLevels = new List<InventoryOrderLevel> { };
            Budgets = new List<InventoryBudget> { };
            LogicalOrders = new List<InventoryLogicalOrder> { };
            DefaultBinLocations = new List<InventoryDefaultBinLocation> { };
            ProductAvailabilities = new List<InventoryProductAvailability> { };
            UpSells = new List<InventoryUpSell> { };
            CrossSells = new List<InventoryCrossSell> { };
            AttributeGroups = new List<InventoryAttributeGroup> { };
            UnitOfMeasures = new List<InventoryUnitOfMeasure> { };
            Images = new List<InventoryImage> { };
            WebStoreCategoryMemberships = new List<InventoryWebStoreCategoryMembership> { };
            TagMemberships = new List<Tag> { };
        }

        public virtual bool? PhysicalItem { get; set; }
        public virtual bool? ShipWithPhysicalItem { get; set; }
        public virtual bool? Discountable { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual decimal? DirectTax { get; set; }
        public virtual InventoryStatuses? Status { get; set; }
        public virtual decimal? DefaultPrice { get; set; }
        public virtual decimal? RRPPrice { get; set; }
        public virtual decimal? LCost { get; set; }
        public virtual decimal? SCost { get; set; }
        public virtual short? DecimalPlaces { get; set; }
        public virtual decimal? MinimumGP { get; set; }
        public virtual decimal? Weight { get; set; }
        public virtual decimal? Cubic { get; set; }
        public virtual bool? UseSerialNo { get; set; }
        public virtual bool? BackOrderable { get; set; }
        public virtual decimal? SalesManCost { get; set; }
        public virtual decimal? SecondaryCost { get; set; }
        public virtual InventoryBOMTypes? BOMObject { get; set; }
        public virtual bool? UseExpiryDate { get; set; }
        public virtual bool? UseStandardCost { get; set; }
        public virtual decimal? StandardCost { get; set; }
        public virtual bool? WebEnabled { get; set; }
        public virtual bool? SellPriceIncTax { get; set; }
        public virtual InventoryStyle? Style { get; set; }
        public virtual InventoryColour? Colour { get; set; }
        public virtual InventorySize? Size { get; set; }
        public virtual int? PartEncodeOrder { get; set; }
        public virtual string? InventoryID { get; set; }
        public virtual string? PartNo { get; set; }
        public virtual byte[] Picture { get; set; }
        public virtual string? Description { get; set; }
        public virtual string? UnitMeasure { get; set; }
        public virtual InventoryClassification? Classification { get; set; }
        public virtual InventoryCategory? Category1 { get; set; }
        public virtual InventoryCategory? Category2 { get; set; }
        public virtual InventoryCategory? Category3 { get; set; }
        public virtual InventoryCategory? Category4 { get; set; }
        public virtual InventoryCategory? Category5 { get; set; }
        public virtual string? Aux1 { get; set; }
        public virtual string? Aux2 { get; set; }
        public virtual string? Aux3 { get; set; }
        public virtual string? Aux4 { get; set; }
        public virtual string? Aux5 { get; set; }
        public virtual string? GSTInwardsID { get; set; }
        public virtual string? GSTInwardsDescription { get; set; }
        public virtual decimal? GSTInwardsRate { get; set; }
        public virtual string? GSTOutwardsID { get; set; }
        public virtual string? GSTOutwardsDescription { get; set; }
        public virtual decimal? GSTOutwardsRate { get; set; }
        public virtual string? GSTAdjustmentsINID { get; set; }
        public virtual string? GSTAdjustmentsINDescription { get; set; }
        public virtual decimal? GSTAdjustmentsINRate { get; set; }
        public virtual string? GSTAdjustmentsOUTID { get; set; }
        public virtual string? GSTAdjustmentsOUTDescription { get; set; }
        public virtual decimal? GSTAdjustmentsOUTRate { get; set; }
        public virtual string? MatrixDescription { get; set; }
        public virtual string? PricingGroupID { get; set; }
        public virtual string? PricingGroupDescription { get; set; }
        public virtual List<InventoryLedger> InventoryLedgers { get; set; }
        public virtual List<Note> Notes { get; set; }
        public virtual List<Document> Documents { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
        public virtual List<InventoryRegion> Regions { get; set; }
        public virtual List<InventoryDebtorPrice> DebtorPrices { get; set; }
        public virtual List<InventoryDebtorClassificationPrice> DebtorClassPrices { get; set; }
        public virtual List<InventoryDebtorPriceGroupInventorySpecific> DebtorPriceGroupInventorySpecificPrices { get; set; }
        public virtual List<InventoryAlternateChild> AlternateChildren { get; set; }
        public virtual List<InventoryAlternateParent> AlternateParents { get; set; }
        public virtual List<InventoryComponent> Components { get; set; }
        public virtual List<InventoryWarehouseSOH> WarehouseSOHs { get; set; }
        public virtual List<InventoryDebtorPartNumber> DebtorPartNumbers { get; set; }
        public virtual List<InventoryGroupMembership> GroupMemberships { get; set; }
        public virtual List<InventoryOtherDescription> OtherDescriptions { get; set; }
        public virtual List<InventoryOrderLevel> OrderLevels { get; set; }
        public virtual List<InventoryBudget> Budgets { get; set; }
        public virtual List<InventoryLogicalOrder> LogicalOrders { get; set; }
        public virtual List<InventoryDefaultBinLocation> DefaultBinLocations { get; set; }
        public virtual List<InventoryProductAvailability> ProductAvailabilities { get; set; }
        public virtual InventorySellingPrices? SellingPrices { get; set; }
        public virtual List<InventoryUpSell> UpSells { get; set; }
        public virtual List<InventoryCrossSell> CrossSells { get; set; }
        public virtual List<InventoryAttributeGroup> AttributeGroups { get; set; }
        public virtual List<InventoryUnitOfMeasure> UnitOfMeasures { get; set; }
        public virtual List<InventoryImage> Images { get; set; }
        public virtual List<InventoryWebStoreCategoryMembership> WebStoreCategoryMemberships { get; set; }
        public virtual List<Tag> TagMemberships { get; set; }
        public virtual string? WebStoreDescription { get; set; }
        public virtual string? WebStoreShortDescription { get; set; }
        public enum InventoryBOMTypes
        {
            e_InventoryBOMTypeNone,
            e_InventoryBOMTypeBOM,
            e_InventoryBOMTypeTemplate,
            e_InventoryBOMTypeKit,
            e_InventoryBOMTypeKitTaxOverride,
        }

        public enum InventoryStatuses
        {
            e_InventoryStatusActive,
            e_InventoryStatusDiscontinued,
            e_InventoryStatusDeleted,
            e_InventoryStatusSlow,
            e_InventoryStatusObsolete,
        }

    }

    public partial class InventoryLedger
    {
        public virtual string? LedgerID { get; set; }
        public virtual string? Name { get; set; }
        public virtual string? LedgerAccountID { get; set; }
        public virtual string? LedgerAccountNo { get; set; }
        public virtual string? LedgerAccountDescription { get; set; }
    }

    public partial class InventoryRegion
    {
        public InventoryRegion()
        {
            Suppliers = new List<InventorySupplier> { };
        }

        public virtual string? RegionSupplierOrderingID { get; set; }
        public virtual bool? OrderEnabled { get; set; }
        public virtual string? RegionID { get; set; }
        public virtual string? RegionName { get; set; }
        public virtual List<InventorySupplier> Suppliers { get; set; }
    }

    public partial class InventoryDebtorPrice
    {
        public virtual PriceSources? Source { get; set; }
        public virtual PriceModes? Mode { get; set; }
        public virtual decimal? Amount { get; set; }
        public virtual DateTime? StartDate { get; set; }
        public virtual DateTime? EndDate { get; set; }
        public virtual bool? UseQuantityPriceBreak { get; set; }
        public virtual decimal? QuantityPriceBreak { get; set; }
        public virtual string? DebtorSpecificPriceID { get; set; }
        public virtual string? DebtorID { get; set; }
        public virtual string? DebtorAccountNo { get; set; }
        public virtual string? DebtorName { get; set; }
        public virtual decimal? Price { get; set; }
        public virtual string? Note { get; set; }
        public enum PriceSources
        {
            SellPrice,
            LastCost,
            RRP,
            P1,
            P2,
            P3,
            P4,
            P5,
            P6,
            P7,
            P8,
            P9,
            P10,
        }

        public enum PriceModes
        {
            Percentage,
            Actual,
            None,
        }

    }

    public partial class InventoryDebtorClassificationPrice
    {
        public virtual InventoryDebtorClassificationPriceSources? Source { get; set; }
        public virtual InventoryDebtorClassificationPriceModes? Mode { get; set; }
        public virtual decimal? Amount { get; set; }
        public virtual DateTime? StartDate { get; set; }
        public virtual DateTime? EndDate { get; set; }
        public virtual bool? UseQuantityPriceBreak { get; set; }
        public virtual decimal? QuantityPriceBreak { get; set; }
        public virtual string? DebtorClassificationPriceID { get; set; }
        public virtual string? DebtorClassificationID { get; set; }
        public virtual string? DebtorClassificationDescription { get; set; }
        public virtual decimal? Price { get; set; }
        public virtual string? Note { get; set; }
        public enum InventoryDebtorClassificationPriceSources
        {
            SellPrice,
            LastCost,
            RRP,
            P1,
            P2,
            P3,
            P4,
            P5,
            P6,
            P7,
            P8,
            P9,
            P10,
        }

        public enum InventoryDebtorClassificationPriceModes
        {
            Percentage,
            Actual,
            None,
        }

    }

    public partial class InventoryDebtorPriceGroupInventorySpecific
    {
        public virtual InventoryDebtorPriceGroupInventorySpecificSources? Source { get; set; }
        public virtual InventoryDebtorPriceGroupInventorySpecificModes? Mode { get; set; }
        public virtual decimal? Amount { get; set; }
        public virtual DateTime? StartDate { get; set; }
        public virtual DateTime? EndDate { get; set; }
        public virtual bool? UseQuantityPriceBreak { get; set; }
        public virtual decimal? QuantityPriceBreak { get; set; }
        public virtual string? DebtorPriceGroupInventorySpecificID { get; set; }
        public virtual string? DebtorPriceGroupID { get; set; }
        public virtual string? DebtorPriceGroupDescription { get; set; }
        public virtual decimal? Price { get; set; }
        public virtual string? Note { get; set; }
        public enum InventoryDebtorPriceGroupInventorySpecificSources
        {
            SellPrice,
            LastCost,
            RRP,
            P1,
            P2,
            P3,
            P4,
            P5,
            P6,
            P7,
            P8,
            P9,
            P10,
        }

        public enum InventoryDebtorPriceGroupInventorySpecificModes
        {
            Percentage,
            Actual,
            None,
        }

    }

    public partial class InventoryAlternateChild
    {
        public virtual string? AlternateChildID { get; set; }
        public virtual string? LinkedInventoryID { get; set; }
        public virtual string? LinkedInventoryPartNo { get; set; }
        public virtual string? LinkedInventoryDescription { get; set; }
        public virtual string? Notes { get; set; }
    }

    public partial class InventoryAlternateParent
    {
        public virtual string? AlternateParentID { get; set; }
        public virtual string? LinkedInventoryID { get; set; }
        public virtual string? LinkedInventoryPartNo { get; set; }
        public virtual string? LinkedInventoryDescription { get; set; }
        public virtual string? Notes { get; set; }
    }

    public partial class InventoryComponent
    {
        public InventoryComponent()
        {
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual decimal? ComponentQuantity { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual string? ComponentID { get; set; }
        public virtual string? ComponentInventoryID { get; set; }
        public virtual string? ComponentInventoryPartNo { get; set; }
        public virtual string? ComponentInventoryDescription { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
    }

    public partial class InventoryWarehouseSOH
    {
        public virtual string? IN_LogicalID { get; set; }
        public virtual string? Warehouse { get; set; }
        public virtual decimal? TotalSOH { get; set; }
        public virtual decimal? TotalBackOrders { get; set; }
        public virtual decimal? ManualBackOrders { get; set; }
        public virtual decimal? AutoBackOrders { get; set; }
        public virtual decimal? ShipOnCompletion { get; set; }
        public virtual decimal? WarehouseTransfers { get; set; }
        public virtual decimal? UnprocessedSales { get; set; }
        public virtual decimal? ForwardRequirements { get; set; }
        public virtual decimal? BOMComponentWIP { get; set; }
    }

    public partial class InventoryDebtorPartNumber
    {
        public virtual string? DebtorPartNumberID { get; set; }
        public virtual string? DebtorID { get; set; }
        public virtual string? DebtorAccountNo { get; set; }
        public virtual string? DebtorName { get; set; }
        public virtual string? DebtorPartNo { get; set; }
        public virtual string? DebtorBarcode { get; set; }
    }

    public partial class InventoryGroupMembership
    {
        public virtual string? GroupMembershipID { get; set; }
        public virtual string? GroupID { get; set; }
        public virtual string? GroupDescription { get; set; }
    }

    public partial class InventoryOtherDescription
    {
        public virtual string? OtherDescriptionID { get; set; }
        public virtual string? Description { get; set; }
    }

    public partial class InventoryOrderLevel
    {
        public virtual DateTime? MonthStartDate { get; set; }
        public virtual DateTime? MonthEndDate { get; set; }
        public virtual decimal? MinSOHUnits { get; set; }
        public virtual decimal? MinSafetySOHUnits { get; set; }
        public virtual decimal? MaxSafetySOHUnits { get; set; }
        public virtual string? LogicalWarehouseID { get; set; }
        public virtual string? LogicalWarehouseDescription { get; set; }
        public virtual string? PhysicalWarehouseID { get; set; }
        public virtual string? PhysicalWarehouseDescription { get; set; }
        public virtual int PeriodNo { get; set; }
    }

    public partial class InventoryBudget
    {
        public virtual int? PeriodNo { get; set; }
        public virtual DateTime? MonthStartDate { get; set; }
        public virtual DateTime? MonthEndDate { get; set; }
        public virtual decimal? BudgetUnits { get; set; }
        public virtual decimal? BudgetValue { get; set; }
        public virtual string? LogicalWarehouseID { get; set; }
        public virtual string? LogicalWarehouseDescription { get; set; }
        public virtual string? PhysicalWarehouseID { get; set; }
        public virtual string? PhysicalWarehouseDescription { get; set; }
    }

    public partial class InventoryLogicalOrder
    {
        public virtual string? LogicalOrderID { get; set; }
        public virtual string? LogicalOrderWarehouseLogicalWarehouseID { get; set; }
        public virtual string? LogicalOrderWarehouseLogicalDescription { get; set; }
        public virtual string? LogicalOrderWarehousePhysicalWarehouseID { get; set; }
        public virtual string? LogicalOrderWarehousePhysicalDescription { get; set; }
        public virtual string? LogicalOrderCentralWarehouseLogicalID { get; set; }
        public virtual string? LogicalOrderCentralWarehouseLogicalDescription { get; set; }
        public virtual string? LogicalOrderCentralWarehousePhysicalID { get; set; }
        public virtual string? LogicalOrderCentralWarehousePhysicalDescription { get; set; }
    }

    public partial class InventoryDefaultBinLocation
    {
        public InventoryDefaultBinLocation()
        {
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual string? DefaultBinLocationID { get; set; }
        public virtual string? LogicalWarehouseID { get; set; }
        public virtual string? LogicalWarehouseDescription { get; set; }
        public virtual string? PhysicalWarehouseID { get; set; }
        public virtual string? PhysicalWarehouseDescription { get; set; }
        public virtual InventoryBinLocation? BinLocation { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
    }
    public partial class InventoryBinLocation
    {
        public InventoryBinLocation()
        {
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual string? BinLocationID { get; set; }
        public virtual string? LogicalWarehouseID { get; set; }
        public virtual string? LogicalWarehouseDescription { get; set; }
        public virtual string? PhysicalWarehouseID { get; set; }
        public virtual string? PhysicalWarehouseDescription { get; set; }
        public virtual string? Description { get; set; }
        public virtual string? ShortName { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual bool? IsDefault { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
    }

    public partial class InventoryProductAvailability
    {
        public virtual bool? Available { get; set; }
        public virtual string? ProductAvailabilityID { get; set; }
        public virtual string? LogicalWarehouseID { get; set; }
        public virtual string? LogicalWarehouseDescription { get; set; }
        public virtual string? PhysicalWarehouseID { get; set; }
        public virtual string? PhysicalWarehouseDescription { get; set; }
    }

    public partial class InventoryUpSell
    {
        public virtual string? UpSellID { get; set; }
        public virtual decimal? UpSellQuantity { get; set; }
        public virtual string? UpSellInventoryID { get; set; }
        public virtual string? UpSellInventoryPartNo { get; set; }
        public virtual string? UpSellInventoryDescription { get; set; }
        public virtual string? UpSellDescription { get; set; }
        public virtual string? PrimaryCategoryID { get; set; }
        public virtual string? PrimaryCategoryDescription { get; set; }
        public virtual int PrimaryCategoryNo { get; set; }
        public virtual string? SecondaryCategoryID { get; set; }
        public virtual string? SecondaryCategoryDescription { get; set; }
        public virtual int? SecondaryCategoryNo { get; set; }
    }

    public partial class InventoryCrossSell
    {
        public virtual string? CrossSellID { get; set; }
        public virtual decimal? CrossSellQuantity { get; set; }
        public virtual string? CrossSellInventoryID { get; set; }
        public virtual string? CrossSellInventoryPartNo { get; set; }
        public virtual string? CrossSellInventoryDescription { get; set; }
        public virtual string? CrossSellDescription { get; set; }
        public virtual string? PrimaryCategoryID { get; set; }
        public virtual string? PrimaryCategoryDescription { get; set; }
        public virtual int PrimaryCategoryNo { get; set; }
        public virtual string? SecondaryCategoryID { get; set; }
        public virtual string? SecondaryCategoryDescription { get; set; }
        public virtual int? SecondaryCategoryNo { get; set; }
    }

    public partial class InventoryAttributeGroup
    {
        public InventoryAttributeGroup()
        {
            Attributes = new List<InventoryAttributeGroupAttribute> { };
        }

        public virtual string? AttributeGroupID { get; set; }
        public virtual InventoryAttributeGroupTemplate? Template { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual string? Description { get; set; }
        public virtual List<InventoryAttributeGroupAttribute> Attributes { get; set; }
    }

    public partial class InventoryAttributeGroupAttribute
    {
        public InventoryAttributeGroupAttribute()
        {
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual string? AttributeID { get; set; }
        public virtual string? AttributeGroupID { get; set; }
        public virtual InventoryAttributeGroupTemplateAttribute? TemplateAttribute { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual string? Contents { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
    }

    public partial class InventoryAttributeGroupTemplate
    {
        public InventoryAttributeGroupTemplate()
        {
            TemplateAttributes = new List<InventoryAttributeGroupTemplateAttribute> { };
        }

        public virtual string? AttributeGroupTemplateID { get; set; }
        public virtual string? Name { get; set; }
        public virtual bool? IsEnabled { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual List<InventoryAttributeGroupTemplateAttribute> TemplateAttributes { get; set; }
    }

    public partial class InventoryAttributeGroupTemplateAttribute
    {
        public InventoryAttributeGroupTemplateAttribute()
        {
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual string? TemplateAttributeID { get; set; }
        public virtual string? AttributeGroupTemplateID { get; set; }
        public virtual int? AttributeType { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual string? Name { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
    }

    public partial class InventoryUnitOfMeasure
    {
        public InventoryUnitOfMeasure()
        {
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual string? RecID { get; set; }
        public virtual InventoryUnitOfMeasure? InnerUnitOfMeasure { get; set; }
        public virtual decimal? QuantityInnersPerUnitOfMeasure { get; set; }
        public virtual bool? IsSell { get; set; }
        public virtual bool? IsDefaultSell { get; set; }
        public virtual bool? IsPurchase { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual string? UnitOfMeasureID { get; set; }
        public virtual string? Name { get; set; }
        public virtual string? PartNo { get; set; }
        public virtual string? Barcode { get; set; }
        public virtual decimal? Length { get; set; }
        public virtual decimal? Width { get; set; }
        public virtual decimal? Height { get; set; }
        public virtual decimal? Volume { get; set; }
        public virtual decimal? Weight { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
        public virtual bool? IsEnabled { get; set; }
    }

    public partial class InventoryImage
    {
        public InventoryImage()
        {
            FileBinary = new byte[] { };
            RowHash = new byte[] { };
        }

        public virtual string? RecID { get; set; }
        public virtual byte[] FileBinary { get; set; }
        public virtual string? AltText { get; set; }
        public virtual string? Title { get; set; }
        public virtual string? Caption { get; set; }
        public virtual string? Description { get; set; }
        public virtual string? PhysicalFileName { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual byte[] RowHash { get; set; }
        public virtual string? WebStore_Image_id { get; set; }
        public virtual string? WebStore_Image_src { get; set; }
        public virtual string? WebStore_Image_name { get; set; }
        public virtual string? WebStore_Image_altText { get; set; }
    }

    public partial class InventoryWebStoreCategoryMembership
    {
        public InventoryWebStoreCategoryMembership()
        {
            RowHash = new byte[] { };
        }

        public virtual string? RecID { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual byte[] RowHash { get; set; }
        public virtual string? WebStoreCategory_RecID { get; set; }
        public virtual string? WebStoreCategory_Name { get; set; }
        public virtual string? WebStoreCategory_WebStore_Category_id { get; set; }
    }

    public partial class InventoryStyle
    {
        public virtual string? StyleID { get; set; }
        public virtual string? StyleCode { get; set; }
        public virtual string? StyleDescription { get; set; }
    }

    public partial class InventoryColour
    {
        public virtual string? ColourID { get; set; }
        public virtual string? Description { get; set; }
    }

    public partial class InventorySize
    {
        public virtual string? SizeID { get; set; }
        public virtual string? Description { get; set; }
    }

    public partial class InventorySellingPrice
    {
        public virtual decimal? Price { get; set; }
        public virtual bool? PriceIsIncTax { get; set; }
        public virtual decimal? ForwardPrice { get; set; }
        public virtual string? SellingPriceID { get; set; }
    }

    public partial class InventorySellingPrices
    {
        public InventorySellingPrices()
        {
            SellPrices = new List<InventorySellingPrice> { };
        }

        public virtual List<InventorySellingPrice> SellPrices { get; set; }
        public virtual DateTime? CurrentPriceDate { get; set; }
        public virtual DateTime? ForwardPriceDate { get; set; }
    }

    public partial class InventorySupplier
    {
        public InventorySupplier()
        {
            CustomFieldValues = new List<CustomFieldValue> { };
            SupplierWarehouses = new List<InventorySupplierWarehouse> { };
            SupplierQuantityPriceBreaks = new List<InventorySupplierQuantityPriceBreak> { };
        }

        public virtual bool? DefaultSupplier { get; set; }
        public virtual decimal? SpareFloat1 { get; set; }
        public virtual decimal? SpareFloat2 { get; set; }
        public virtual decimal? SpareFloat3 { get; set; }
        public virtual DateTime? SpareDate1 { get; set; }
        public virtual DateTime? SpareDate2 { get; set; }
        public virtual DateTime? SpareDate3 { get; set; }
        public virtual string? SupplierID { get; set; }
        public virtual string? CreditorID { get; set; }
        public virtual string? CreditorAccountNo { get; set; }
        public virtual string? CreditorName { get; set; }
        public virtual string? SupplierPartNo { get; set; }
        public virtual string? SupplierUPC { get; set; }
        public virtual string? SpareString1 { get; set; }
        public virtual string? SpareString2 { get; set; }
        public virtual string? SpareString3 { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
        public virtual List<InventorySupplierWarehouse> SupplierWarehouses { get; set; }
        public virtual List<InventorySupplierQuantityPriceBreak> SupplierQuantityPriceBreaks { get; set; }
    }

    public partial class InventorySupplierQuantityPriceBreak
    {
        public virtual decimal? QuantityBreak { get; set; }
        public virtual decimal? Price { get; set; }
        public virtual string? SupplierQuantityPriceBreakID { get; set; }
        public virtual string? CurrencyID { get; set; }
    }
    public partial class InventorySupplierWarehouse
    {
        public InventorySupplierWarehouse()
        {
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual bool? DefaultWarehouse { get; set; }
        public virtual decimal? HomeSuppliersCost { get; set; }
        public virtual decimal? SuppliersCost { get; set; }
        public virtual decimal? SuppliersCost2 { get; set; }
        public virtual decimal? HomeSuppliersCost2 { get; set; }
        public virtual decimal? SupplierSOH { get; set; }
        public virtual int? DeliveryDays { get; set; }
        public virtual decimal? SpareFloat1 { get; set; }
        public virtual decimal? SpareFloat2 { get; set; }
        public virtual decimal? SpareFloat3 { get; set; }
        public virtual DateTime? SpareDate1 { get; set; }
        public virtual DateTime? SpareDate2 { get; set; }
        public virtual DateTime? SpareDate3 { get; set; }
        public virtual string? SupplierWarehouseID { get; set; }
        public virtual string? CreditorWarehouseID { get; set; }
        public virtual string? CreditorWarehouseDescription { get; set; }
        public virtual string? SpareString1 { get; set; }
        public virtual string? SpareString2 { get; set; }
        public virtual string? SpareString3 { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
        public virtual InventoryUnitOfMeasure? UnitOfMeasure { get; set; }
        public virtual string? CurrencyID { get; set; }
        public virtual decimal? OrderUnits { get; set; }
    }
}

namespace JiwaFinancials.Jiwa.JiwaServiceModel.Inventory.Category
{
    public partial class InventoryCategory
    {
        public InventoryCategory()
        {
            Picture = new byte[] { };
            CustomFieldValues = new List<CustomFieldValue> { };
        }

        public virtual string? CategoryID { get; set; }
        public virtual int? CategoryNo { get; set; }
        public virtual string? Description { get; set; }
        public virtual bool? IsDefault { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual byte[] Picture { get; set; }
        public virtual List<CustomFieldValue> CustomFieldValues { get; set; }
    }

}

namespace JiwaFinancials.Jiwa.JiwaServiceModel.Inventory.Classification
{
    public partial class InventoryClassification
    {
        public InventoryClassification()
        {
            InventoryLedgers = new List<InventoryLedger> { };
            CustomFields = new List<CustomFieldValue> { };
            Picture = new byte[] { };
        }

        public virtual string? ClassificationID { get; set; }
        public virtual string? Description { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual bool? WebEnabled { get; set; }
        public virtual bool? IsDefault { get; set; }
        public virtual string? GSTInwardsTaxRateID { get; set; }
        public virtual string? GSTInwardsTaxRateDescription { get; set; }
        public virtual decimal? GSTInwardsTaxRate { get; set; }
        public virtual string? GSTOutwardsTaxRateID { get; set; }
        public virtual string? GSTOutwardsTaxRateDescription { get; set; }
        public virtual decimal? GSTOutwardsTaxRate { get; set; }
        public virtual string? GSTAdjustmentsINTaxRateID { get; set; }
        public virtual string? GSTAdjustmentsINTaxRateDescription { get; set; }
        public virtual decimal? GSTAdjustmentsINTaxRate { get; set; }
        public virtual string? GSTAdjustmentsOUTTaxRateID { get; set; }
        public virtual string? GSTAdjustmentsOUTTaxRateDescription { get; set; }
        public virtual decimal? GSTAdjustmentsOUTTaxRate { get; set; }
        public virtual List<InventoryLedger> InventoryLedgers { get; set; }
        public virtual string? PricingGroupID { get; set; }
        public virtual string? PricingGroupDescription { get; set; }
        public virtual List<CustomFieldValue> CustomFields { get; set; }
        public virtual byte[] Picture { get; set; }
    }
}
#endregion

#region "Notes"
namespace JiwaFinancials.Jiwa.JiwaServiceModel.Notes
{
    public partial class Note
    {
        public virtual string? NoteID { get; set; }
        public virtual NoteType? NoteType { get; set; }
        public virtual int? LineNo { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual string? LastModifiedByStaffID { get; set; }
        public virtual string? LastModifiedByStaffUsername { get; set; }
        public virtual string? LastModifiedByStaffTitle { get; set; }
        public virtual string? LastModifiedByStaffFirstName { get; set; }
        public virtual string? LastModifiedByStaffSurname { get; set; }
        public virtual string? NoteText { get; set; }
        public virtual byte[]? RowHash { get; set; }
    }

    public partial class NoteType
    {
        public virtual string? NoteTypeID { get; set; }
        public virtual string? Description { get; set; }
        public virtual bool? DefaultType { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual byte[]? RowHash { get; set; }
    }
}

#endregion

#region "Sales Order"
namespace JiwaFinancials.Jiwa.JiwaServiceModel.SalesOrders
{
    public partial class CartageCharge
    {
        public virtual decimal? ExTaxAmount { get; set; }
        public virtual decimal? FXExTaxAmount { get; set; }
        public virtual decimal? TaxAmount { get; set; }
        public virtual decimal? FXTaxAmount { get; set; }
        public virtual TaxRate? TaxRate { get; set; }
    }

    public partial class CreditReason
    {
        public virtual string? CreditReasonID { get; set; }
        public virtual string? CreditReasonDescription { get; set; }
        public virtual bool? CreditIntoStock { get; set; }
        public virtual bool? IsEnabled { get; set; }
        public virtual bool? IsDefault { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual int? ItemNo { get; set; }
    }

    public partial class DeliveryMethod
    {
        public virtual string? RecID { get; set; }
        public virtual string? Name { get; set; }
        public virtual bool? IsEnabled { get; set; }
        public virtual bool? IsDefault { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual byte[]? RowHash { get; set; }
    }

    public partial class Origin
    {
        public virtual string? RecID { get; set; }
        public virtual string? Name { get; set; }
        public virtual bool? IsEnabled { get; set; }
        public virtual bool? IsDefault { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual byte[]? RowHash { get; set; }
    }

    public partial class PaymentType
    {
        public virtual string? PaymentTypeID { get; set; }
        public virtual string? Name { get; set; }
        public virtual string? Code { get; set; }
        public virtual int? ItemNo { get; set; }
        public virtual bool? IsEnabled { get; set; }
        public virtual bool? IsDefault { get; set; }
        public virtual bool? IsCreditCard { get; set; }
        public virtual bool? IsPOS { get; set; }
        //public virtual BankAccount BankAccount { get; set; } // Don't need this, so we don't include it
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
    }

    public partial class SalesOrder
    {
        public virtual string? Type { get; set; }
        public virtual SalesOrderSystemSettings? SystemSettings { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual DateTime? InitiatedDate { get; set; }
        public virtual DateTime? InvoiceInitDate { get; set; }
        public virtual SalesOrderTypes? SalesOrderType { get; set; }
        public virtual SalesOrderOrderTypes? OrderType { get; set; }
        public virtual SalesOrderStatuses? Status { get; set; }
        public virtual SalesOrderEDIPickStatuses? EDIStatus { get; set; }
        public virtual SalesOrderBillTypes? BillType { get; set; }
        public virtual DateTime? ExpectedDeliveryDate { get; set; }
        public virtual DateTime? DeliveredDate { get; set; }
        public virtual bool? Delivered { get; set; }
        public virtual SalesOrderEDIPickStatuses? EDIPickStatus { get; set; }
        public virtual SalesOrderEDIOrderTypes? EDIOrderType { get; set; }
        public virtual DateTime? EDIDeliverNotBeforeDate { get; set; }
        public virtual DateTime? EDIDeliverNotAfterDate { get; set; }
        public virtual SalesOrderCashSales? CashSales { get; set; }
        public virtual bool? DropShipment { get; set; }
        public virtual decimal? Cartage1ExGst { get; set; }
        public virtual decimal? FXCartage1ExGst { get; set; }
        public virtual decimal? Cartage1GstRate { get; set; }
        public virtual decimal? Cartage1Gst { get; set; }
        public virtual decimal? FXCartage1Gst { get; set; }
        public virtual decimal? Cartage2ExGst { get; set; }
        public virtual decimal? FXCartage2ExGst { get; set; }
        public virtual decimal? Cartage2GstRate { get; set; }
        public virtual decimal? Cartage2Gst { get; set; }
        public virtual decimal? FXCartage2Gst { get; set; }
        public virtual decimal? Cartage3ExGst { get; set; }
        public virtual decimal? FXCartage3ExGst { get; set; }
        public virtual decimal? Cartage3GstRate { get; set; }
        public virtual decimal? Cartage3Gst { get; set; }
        public virtual decimal? FXCartage3Gst { get; set; }
        public virtual decimal? RCTIAmount { get; set; }
        public virtual decimal? FXRCTIAmount { get; set; }
        public virtual DateTime? RCTIDate { get; set; }
        public virtual SalesOrderJobCosting? JobCosting { get; set; }
        public virtual string? InvoiceID { get; set; }
        public virtual string? InvoiceNo { get; set; }
        public virtual string? LogicalID { get; set; }
        public virtual string? LogicalWarehouseDescription { get; set; }
        public virtual string? PhysicalWarehouseDescription { get; set; }
        public virtual bool? CreditNote { get; set; }
        public virtual string? StaffID { get; set; }
        public virtual string? StaffUserName { get; set; }
        public virtual string? StaffTitle { get; set; }
        public virtual string? StaffFirstName { get; set; }
        public virtual string? StaffSurname { get; set; }
        public virtual string? BranchID { get; set; }
        public virtual string? BranchDescription { get; set; }
        public virtual string? BranchName { get; set; }
        public virtual string? OrderNo { get; set; }
        public virtual string? SOReference { get; set; }
        public virtual string? SenderEDIAddress { get; set; }
        public virtual string? ReceiverEDIAddress { get; set; }
        public virtual string? EDIVendorNumber { get; set; }
        public virtual string? EDIBuyerNumber { get; set; }
        public virtual string? DebtorID { get; set; }
        public virtual string? DebtorAccountNo { get; set; }
        public virtual string? DebtorName { get; set; }
        public virtual string? DebtorEmailAddress { get; set; }
        public virtual string? DebtorContactName { get; set; }
        public virtual string? EDIASN { get; set; }
        public virtual string? DeliveryAddressee { get; set; }
        public virtual string? DeliveryAddressPhone { get; set; }
        public virtual string? DeliveryAddress1 { get; set; }
        public virtual string? DeliveryAddress2 { get; set; }
        public virtual string? DeliveryAddressSuburb { get; set; }
        public virtual string? DeliveryAddressState { get; set; }
        public virtual string? DeliveryAddressContactName { get; set; }
        public virtual string? DeliveryAddressPostcode { get; set; }
        public virtual string? DeliveryAddressCountry { get; set; }
        public virtual decimal? DeliveryAddressLatitude { get; set; }
        public virtual decimal? DeliveryAddressLongitude { get; set; }
        public virtual string? DeliveryAddressNotes { get; set; }
        public virtual string? DeliveryAddressCourierDetails { get; set; }
        public virtual string? DeliveryAddressEmailAddress { get; set; }
        public virtual string? RCTINo { get; set; }
        public virtual List<CustomFieldValue>? CustomFieldValues { get; set; }
        public virtual List<Note>? Notes { get; set; }
        public virtual List<Document>? Documents { get; set; }
        public virtual List<SalesOrderPayment>? Payments { get; set; }
        public virtual List<SalesOrderLine>? Lines { get; set; }
        public virtual List<SalesOrderHistory>? Histories { get; set; }
        public virtual List<SalesOrderASN>? ASNs { get; set; }
        public virtual Origin? Origin { get; set; }
        public virtual DeliveryMethod? DeliveryMethod { get; set; }
        public virtual CreditReason? CreditReason { get; set; }
        public virtual string? CreditNoteFromInvoiceHistoryID { get; set; }
        public virtual string? CurrencyID { get; set; }
        public virtual string? CurrencyShortName { get; set; }
        public virtual decimal? CurrencyRate { get; set; }
        public enum SalesOrderTypes
        {
            e_SalesOrderNormalSalesOrder,
            e_SalesOrderBackToBack,
        }

        public enum SalesOrderOrderTypes
        {
            e_SalesOrderOrderTypeReserveOrder,
            e_SalesOrderOrderTypeInvoiceOrder,
            e_SalesOrderOrderTypeForwardOrder,
            e_SalesOrderOrderTypeActiveOrder,
        }

        public enum SalesOrderStatuses
        {
            e_SalesOrderEntered,
            e_SalesOrderProcessed,
            e_SalesOrderClosed,
            e_SalesOrderUnprocessedPrinted,
        }

        public enum SalesOrderEDIPickStatuses
        {
            e_SalesOrderHistoryEDIPickStatusNone,
            e_SalesOrderHistoryEDIPickStatusPOReceived,
            e_SalesOrderHistoryEDIPickStatusPOAcknowledgementReadyToSend,
            e_SalesOrderHistoryEDIPickStatusPOAcknowledgementSent,
            e_SalesOrderHistoryEDIPickStatusReadyToBePicked,
            e_SalesOrderHistoryEDIPickStatusPicking,
            e_SalesOrderHistoryEDIPickStatusPicked,
            e_SalesOrderHistoryEDIPickStatusASNReadyToSend,
            e_SalesOrderHistoryEDIPickStatusASNSent,
            e_SalesOrderHistoryEDIPickStatusRCTIReceived,
            e_SalesOrderHistoryEDIPickStatusError,
            e_SalesOrderHistoryEDIPickStatusRejectionReadyToSend,
            e_SalesOrderHistoryEDIPickStatusRejectionSent,
        }

        public enum SalesOrderBillTypes
        {
            e_SalesOrderShipAndBill,
            e_SalesOrderBillWhenComplete,
            e_SalesOrderShipWhenComplete,
        }

        public enum SalesOrderEDIOrderTypes
        {
            e_SalesOrderEDIOrderTypeNormal,
            e_SalesOrderEDIOrderTypeConsolidated,
        }

    }

    public partial class SalesOrderASN
    {
        public virtual string? ASNNo { get; set; }
        public virtual string? PurchaseOrderNo { get; set; }
        public virtual string? ReceiptNo { get; set; }
        public virtual decimal? GrossAmount { get; set; }
        public virtual decimal? TotalGSTAmount { get; set; }
        public virtual DateTime? ReceiptDate { get; set; }
    }

    public partial class SalesOrderCarrier
    {
        public virtual string? CarrierID { get; set; }
        public virtual string? CarrierName { get; set; }
        public virtual string? AccountNo { get; set; }
        public virtual SalesOrderCarrierService? Service { get; set; }
        public virtual bool? UseLeastCost { get; set; }
        public virtual FreightChargeTos? ChargeTo { get; set; }
        public virtual FreightSystemStatuses? Status { get; set; }
        public virtual List<SalesOrderFreightItem>? FreightItemCollection { get; set; }
        public virtual List<SalesOrderConsignmentNote>? ConsignmentNoteCollection { get; set; }
        public enum FreightSystemStatuses
        {
            FreightSystemStatusNone,
            FreightSystemStatusReadyToSend,
            FreightSystemStatusSent,
            FreightSystemStatusCompleted,
        }

        public enum FreightChargeTos
        {
            FreightChargeToSender,
            FreightChargeToReceiver,
        }

    }

    public partial class SalesOrderCarrierFreightDescription
    {
        public virtual string? CarrierFreightDescriptionID { get; set; }
        public virtual string? Description { get; set; }
    }

    public partial class SalesOrderCarrierService
    {
        public virtual string? CarrierServiceID { get; set; }
        public virtual string? Name { get; set; }
        public virtual decimal? MaximumWeight { get; set; }
    }

    public partial class SalesOrderCashSales
    {
        public virtual string? Name { get; set; }
        public virtual string? Company { get; set; }
        public virtual string? Address1 { get; set; }
        public virtual string? Address2 { get; set; }
        public virtual string? Address3 { get; set; }
        public virtual string? Address4 { get; set; }
        public virtual string? PostCode { get; set; }
        public virtual string? Phone { get; set; }
        public virtual string? Fax { get; set; }
        public virtual string? ContactName { get; set; }
        public virtual string? Country { get; set; }
        public virtual string? EmailAddress { get; set; }
    }

    public partial class SalesOrderConsignmentNote
    {
        public virtual string? ConsignmentNoteID { get; set; }
        public virtual DateTime? ConsignmentNoteDate { get; set; }
        public virtual decimal? ExGSTAmount { get; set; }
        public virtual decimal? GSTAmount { get; set; }
        public virtual string? ConsignmentNoteNo { get; set; }
        public virtual decimal? IncGSTAmount { get; set; }
    }

    public partial class SalesOrderFreightItem
    {
        public virtual string? FreightItemID { get; set; }
        public virtual int? NumberItems { get; set; }
        public virtual decimal? ItemWeight { get; set; }
        public virtual decimal? ItemCubic { get; set; }
        public virtual decimal? ItemLength { get; set; }
        public virtual decimal? ItemWidth { get; set; }
        public virtual decimal? ItemHeight { get; set; }
        public virtual string? Reference { get; set; }
        public virtual SalesOrderCarrierFreightDescription? FreightDescription { get; set; }
        public virtual SalesOrderConsignmentNote? ConsignmentNote { get; set; }
        public virtual List<CustomFieldValue>? CustomFieldValues { get; set; }
    }

    public partial class SalesOrderHistory
    {
        public virtual string? InvoiceHistoryID { get; set; }
        public virtual int? HistoryNo { get; set; }
        public virtual SalesOrderHistoryStatuses? Status { get; set; }
        public virtual SalesOrderHistoryEDIPickStatuses? EDIPickStatus { get; set; }
        public virtual string? DBTransID { get; set; }
        public virtual string? Ref { get; set; }
        public virtual string? LastModifiedBy { get; set; }
        public virtual decimal? HistoryTotal { get; set; }
        public virtual decimal? AmountPaid { get; set; }
        public virtual decimal? FXAmountPaid { get; set; }
        public virtual decimal? TotalQuantityDelivered { get; set; }
        public virtual string? RunNo { get; set; }
        public virtual bool? Delivered { get; set; }
        public virtual DateTime? DeliveredDate { get; set; }
        public virtual DateTime? RecordDate { get; set; }
        public virtual DateTime? DateCreated { get; set; }
        public virtual DateTime? DateLastSaved { get; set; }
        public virtual DateTime? DatePosted { get; set; }
        public virtual DateTime? DateProcessed { get; set; }
        public virtual bool? InvoicePrinted { get; set; }
        public virtual bool? DocketPrinted { get; set; }
        public virtual bool? PackSlipPrinted { get; set; }
        public virtual bool? PickSheetPrinted { get; set; }
        public virtual bool? OtherPrinted { get; set; }
        public virtual bool? InvoiceEmailed { get; set; }
        public virtual bool? DocketEmailed { get; set; }
        public virtual bool? PackSlipEmailed { get; set; }
        public virtual bool? PickSheetEmailed { get; set; }
        public virtual bool? OtherEmailed { get; set; }
        public virtual string? DeliveryAddressContactName { get; set; }
        public virtual string? DeliveryAddressee { get; set; }
        public virtual string? DeliveryAddressPhone { get; set; }
        public virtual string? DeliveryAddress1 { get; set; }
        public virtual string? DeliveryAddress2 { get; set; }
        public virtual string? DeliveryAddress3 { get; set; }
        public virtual string? DeliveryAddress4 { get; set; }
        public virtual string? DeliveryAddressPostcode { get; set; }
        public virtual string? DeliveryAddressCountry { get; set; }
        public virtual decimal? DeliveryAddressLatitude { get; set; }
        public virtual decimal? DeliveryAddressLongitude { get; set; }
        public virtual string? Notes { get; set; }
        public virtual string? CourierDetails { get; set; }
        public virtual string? DeliveryAddressEmailAddress { get; set; }
        public virtual string? FreightForwardAddressPhone { get; set; }
        public virtual string? FreightForwardAddress1 { get; set; }
        public virtual string? FreightForwardAddress2 { get; set; }
        public virtual string? FreightForwardAddress3 { get; set; }
        public virtual string? FreightForwardAddress4 { get; set; }
        public virtual string? FreightForwardAddressPostcode { get; set; }
        public virtual string? FreightForwardAddressCountry { get; set; }
        public virtual decimal? FreightForwardAddressLatitude { get; set; }
        public virtual decimal? FreightForwardAddressLongitude { get; set; }
        public virtual string? FreightForwardAddressNotes { get; set; }
        public virtual string? FreightForwardAddressCourierDetails { get; set; }
        public virtual string? FreightForwardAddressEmailAddress { get; set; }
        public virtual string? ConsignmentNote { get; set; }
        public virtual string? EDIASNNumber { get; set; }
        public virtual bool? DropShipment { get; set; }
        public virtual CartageCharge? CartageCharge1 { get; set; }
        public virtual CartageCharge? CartageCharge2 { get; set; }
        public virtual CartageCharge? CartageCharge3 { get; set; }
        public virtual SalesOrderCarrier? Carrier { get; set; }
        public virtual List<CustomFieldValue>? CustomFieldValues { get; set; }
        public virtual StaffMember? ProcessedBy { get; set; }
        public virtual List<SalesOrderLine>? Lines { get; set; }
        public enum SalesOrderHistoryStatuses
        {
            e_SalesOrderHistoryStatusEntering,
            e_SalesOrderHistoryStatusEntered,
            e_SalesOrderHistoryStatusReadyForPicking,
            e_SalesOrderHistoryStatusPicking,
            e_SalesOrderHistoryStatusPicked,
            e_SalesOrderHistoryStatusDelivery,
            e_SalesOrderHistoryStatusDelivered,
            e_SalesOrderHistoryStatusInvoicing,
            e_SalesOrderHistoryStatusInvoiced,
        }

        public enum SalesOrderHistoryEDIPickStatuses
        {
            e_SalesOrderHistoryEDIPickStatusNone,
            e_SalesOrderHistoryStatuse_SalesOrderHistoryEDIPickStatusPOReceivedEntered,
            e_SalesOrderHistoryEDIPickStatusPOAcknowledgementReadyToSend,
            e_SalesOrderHistoryEDIPickStatusPOAcknowledgementSent,
            e_SalesOrderHistoryEDIPickStatusReadyToBePicked,
            e_SalesOrderHistoryEDIPickStatusPicking,
            e_SalesOrderHistoryEDIPickStatusPicked,
            e_SalesOrderHistoryEDIPickStatusASNReadyToSend,
            e_SalesOrderHistoryEDIPickStatusASNSent,
            e_SalesOrderHistoryEDIPickStatusRCTIReceived,
            e_SalesOrderHistoryEDIPickStatusError,
            e_SalesOrderHistoryEDIPickStatusRejectionReadyToSend,
            e_SalesOrderHistoryEDIPickStatusRejectionSent,
        }

    }

    public partial class SalesOrderJobCosting
    {
        public virtual bool? GSTApplicable { get; set; }
        public virtual string? JobCostID { get; set; }
        public virtual string? JobCostNo { get; set; }
        public virtual string? Description { get; set; }
    }

    public partial class SalesOrderLine
    {
        public virtual int? ItemNo { get; set; }
        public virtual bool? CommentLine { get; set; }
        public virtual decimal? QuantityOrdered { get; set; }
        virtual public decimal? QuantityPreviousDemand { get; set; }
        public virtual decimal? QuantityDemand { get; set; }
        virtual public decimal? QuantityPreviousDelivery { get; set; }
        public virtual decimal? QuantityThisDel { get; set; }
        public virtual decimal? QuantityBackOrd { get; set; }
        public virtual bool? Picked { get; set; }
        public virtual decimal? PriceExGst { get; set; }
        public virtual decimal? FXPriceExGst { get; set; }
        public virtual decimal? PriceIncGst { get; set; }
        public virtual decimal? FXPriceIncGst { get; set; }
        public virtual decimal? DiscountedPrice { get; set; }
        public virtual decimal? FXDiscountedPrice { get; set; }
        public virtual decimal? TaxToCharge { get; set; }
        public virtual decimal? FXTaxToCharge { get; set; }
        public virtual TaxRate? TaxRate { get; set; }
        public virtual decimal? UnitCost { get; set; }
        public virtual bool? FixSellPrice { get; set; }
        public virtual bool? FixPrice { get; set; }
        public virtual decimal? UserDefinedFloat1 { get; set; }
        public virtual decimal? UserDefinedFloat2 { get; set; }
        public virtual decimal? UserDefinedFloat3 { get; set; }
        public virtual DateTime? ForwardOrderDate { get; set; }
        public virtual DateTime? ScheduledDate { get; set; }
        public virtual decimal? LineTotal { get; set; }
        public virtual decimal? FXLineTotal { get; set; }
        public virtual decimal? Weight { get; set; }
        public virtual decimal? Cubic { get; set; }
        public virtual decimal? QuotedDiscountedPrice { get; set; }
        public virtual decimal? FXQuotedDiscountedPrice { get; set; }
        public virtual decimal? QuotedDiscountPercentage { get; set; }
        public virtual decimal? DiscountedPercentage { get; set; }
        public virtual decimal? DiscountGiven { get; set; }
        public virtual decimal? FXDiscountGiven { get; set; }
        public virtual short? QuantityDecimalPlaces { get; set; }
        public virtual decimal? QuantityOriginalOrdered { get; set; }
        public virtual SalesOrderSerialStockSelectionTypesEnum? SalesOrderSerialStockSelectionTypes { get; set; }
        public virtual bool? NonInventory { get; set; }
        virtual public string? PreviousSnapInvoiceLineID { get; set; }
        public virtual string? InvoiceLineID { get; set; }
        public virtual string? InventoryID { get; set; }
        public virtual string? PartNo { get; set; }
        public virtual string? Description { get; set; }
        public virtual string? CommentText { get; set; }
        public virtual string? Aux2 { get; set; }
        public virtual string? LineLinkID { get; set; }
        public virtual string? EDIStoreLocationCode { get; set; }
        public virtual string? EDIDCLocationCode { get; set; }
        public virtual string? CostCenter { get; set; }
        public virtual string? Stage { get; set; }
        public virtual List<CustomFieldValue>? CustomFieldValues { get; set; }
        public virtual List<SalesOrderLineDetail>? LineDetails { get; set; }
        public virtual List<SalesOrderShippingLabel>? ShippingLabels { get; set; }
        public virtual InventoryUnitOfMeasure? UnitOfMeasure { get; set; }
        public virtual SalesOrderKitLineTypesEnum? KitLineType { get; set; }
        public virtual decimal? KitUnits { get; set; }
        public virtual string? KitHeaderLineID { get; set; }
        public virtual string? SKUUnitName { get; set; }

        public enum SalesOrderSerialStockSelectionTypesEnum
        {
            e_SalesOrderSerialStockSelectionPrompted,
            e_SalesOrderSerialStockSelectionFIFO,
        }

        public enum SalesOrderKitLineTypesEnum
        {
            e_SalesOrderNormalLine,
            e_SalesOrderKitHeader,
            e_SalesOrderKitComponent,
        }

    }

    public partial class SalesOrderLineDetail
    {
        public virtual decimal? Cost { get; set; }
        public virtual DateTime? DateIn { get; set; }
        public virtual DateTime? ExpiryDate { get; set; }
        public virtual decimal? SpecialPrice { get; set; }
        public virtual decimal? Quantity { get; set; }
        public virtual string? LineDetailID { get; set; }
        public virtual string? BinLocationID { get; set; }
        public virtual string? BinLocation { get; set; }
        public virtual string? BinLocationShortName { get; set; }
        public virtual string? SerialNo { get; set; }
        public virtual string? SOHID { get; set; }
        public virtual string? IN_LogicalID { get; set; }
    }

    public partial class SalesOrderPayment
    {
        public virtual int? HistoryNo { get; set; }
        public virtual PaymentType? PaymentType { get; set; }
        public virtual decimal? AmountPaid { get; set; }
        public virtual decimal? FXAmountPaid { get; set; }
        public virtual DateTime? PaymentDate { get; set; }
        public virtual bool? ProcessPayment { get; set; }
        public virtual PaymentAuthStatuses? AuthorisationStatus { get; set; }
        public virtual int? PaymentGatewayReturnCode { get; set; }
        public virtual bool? Processed { get; set; }
        public virtual DateTime? CardExpiry { get; set; }
        public virtual string? PaymentID { get; set; }
        public virtual string? PaymentRef { get; set; }
        public virtual string? AuthorisationNumber { get; set; }
        public virtual string? PaymentGatewayReturnMessage { get; set; }
        public virtual string? CardNumber { get; set; }
        public virtual string? CardHolder { get; set; }
        public virtual string? BankName { get; set; }
        public virtual string? BSBN { get; set; }
        public virtual string? BankAcc { get; set; }
        public virtual string? AccountName { get; set; }
        public enum PaymentAuthStatuses
        {
            NoAuthorisationNeeded,
            AuthorisationRequired,
            Authorised,
            Declined,
            Error,
        }

    }

    public partial class SalesOrderShippingLabel
    {
        public virtual decimal? Quantity { get; set; }
        public virtual DateTime? UseByDate { get; set; }
        public virtual int? LabelNumber { get; set; }
        public virtual decimal? SpareNumeric1 { get; set; }
        public virtual decimal? SpareNumeric2 { get; set; }
        public virtual decimal? SpareNumeric3 { get; set; }
        public virtual DateTime? SpareDate1 { get; set; }
        public virtual DateTime? SpareDate2 { get; set; }
        public virtual DateTime? SpareDate3 { get; set; }
        public virtual string? ShippingLabelID { get; set; }
        public virtual string? SSCCNumber { get; set; }
        public virtual string? BatchNo { get; set; }
        public virtual string? Reference { get; set; }
        public virtual string? SpareString1 { get; set; }
        public virtual string? SpareString2 { get; set; }
        public virtual string? SpareString3 { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
    }

    public partial class SalesOrderSystemSettings
    {
        public virtual bool? ForceInventorySelection { get; set; }
        public virtual bool? SuppressLineRetotalling { get; set; }
        public virtual bool? IgnoreDebtorOnHold { get; set; }
        public virtual bool? CompensateTaxRounding { get; set; }
    }

}
#endregion

#region "Staff"
namespace JiwaFinancials.Jiwa.JiwaServiceModel.Staff
{
    public partial class StaffMember
    {
        public virtual string? StaffID { get; set; }
        public virtual string? Title { get; set; }
        public virtual string? FirstName { get; set; }
        public virtual string? Surname { get; set; }
        public virtual string? Username { get; set; }
        public virtual bool? IsActive { get; set; }
        public virtual bool? IsEnabled { get; set; }
    }

}
#endregion

#region "Tags"
namespace JiwaFinancials.Jiwa.JiwaServiceModel.Tags
{
    public partial class Tag
    {
        public virtual string? RecID { get; set; }
        public virtual string? Text { get; set; }
        public virtual int? Colour { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual byte[]? RowHash { get; set; }
        public virtual int? ItemNo { get; set; }
    }
}
#endregion

#region "Tax"
namespace JiwaFinancials.Jiwa.JiwaServiceModel.Tax
{
    public partial class TaxRate
    {
        public virtual string? RecID { get; set; }
        public virtual string? TaxID { get; set; }
        public virtual string? Description { get; set; }
        public virtual TaxRateTypes? GSTTaxGroup { get; set; }
        public virtual decimal? Rate { get; set; }
        public virtual bool? IsDefaultRate { get; set; }
        public virtual decimal? BASCode { get; set; }
        public virtual bool? IsDefaultRateInGroup { get; set; }
        public virtual bool? IsEnabled { get; set; }
        public virtual Account? LedgerAccount { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual List<CustomFieldValue>? CustomFieldValues { get; set; }
    }

    public enum TaxRateTypes
    {
        WST,
        GSTIn,
        GSTOut,
        GSTAdjustmentsIn,
        GSTAdjustmentsOut,
    }

}
#endregion
#endregion

#region "AutoQueries and Tables"
namespace JiwaFinancials.Jiwa.JiwaServiceModel.Tables
{
    #region "Debtors"
    public partial class v_Jiwa_Debtor_List
    {
        [Required]
        public string? DebtorID { get; set; }
        [Required]
        public string? AccountNo { get; set; }
        public string? Name { get; set; }
        public string? AltAccountNo { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Address4 { get; set; }
        public string? PostCode { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
        public bool? AccountOnHold { get; set; }
        public string? EmailAddress { get; set; }
        public decimal? CurrentBalance { get; set; }
        [Required]
        public bool WebAccess { get; set; }
        [Required]
        public DateTimeOffset LastSavedDateTime { get; set; }
        public byte? TradingStatus { get; set; }
        [Required]
        public string? DebtorClassificationID { get; set; }
        [Required]
        public string? ClassificationDescription { get; set; }
        [Required]
        public string? Category1ID { get; set; }
        public string? Category1Description { get; set; }
        [Required]
        public string? Category2ID { get; set; }
        public string? Category2Description { get; set; }
        [Required]
        public string? Category3ID { get; set; }
        public string? Category3Description { get; set; }
        [Required]
        public string? Category4ID { get; set; }
        public string? Category4Description { get; set; }
        [Required]
        public string? Category5ID { get; set; }
        public string? Category5Description { get; set; }
        [Required]
        public string? PriceSchemeID { get; set; }
        [Required]
        public string? PriceSchemeDescription { get; set; }
        [Required]
        public string? PricingGroupDescription { get; set; }
    }

    [Route("/Queries/DebtorList", "GET")]
    [ApiResponse(200, "Read OK")]
    [ApiResponse(401, "Not authenticated")]
    [ApiResponse(403, "Not authorised")]
    public partial class v_Jiwa_Debtor_ListQuery : QueryDb<v_Jiwa_Debtor_List>
    {
        public string? DebtorID { get; set; }

        public string? DebtorIDStartsWith { get; set; }
        public string? DebtorIDEndsWith { get; set; }
        public string? DebtorIDContains { get; set; }
        public string? DebtorIDLike { get; set; }
        public string?[]? DebtorIDBetween { get; set; }
        public string?[]? DebtorIDIn { get; set; }

        public string? AccountNo { get; set; }

        public string? AccountNoStartsWith { get; set; }
        public string? AccountNoEndsWith { get; set; }
        public string? AccountNoContains { get; set; }
        public string? AccountNoLike { get; set; }
        public string?[]? AccountNoBetween { get; set; }
        public string?[]? AccountNoIn { get; set; }

        public string? Name { get; set; }

        public string? NameStartsWith { get; set; }
        public string? NameEndsWith { get; set; }
        public string? NameContains { get; set; }
        public string? NameLike { get; set; }
        public string?[]? NameBetween { get; set; }
        public string?[]? NameIn { get; set; }

        public string? AltAccountNo { get; set; }

        public string? AltAccountNoStartsWith { get; set; }
        public string? AltAccountNoEndsWith { get; set; }
        public string? AltAccountNoContains { get; set; }
        public string? AltAccountNoLike { get; set; }
        public string?[]? AltAccountNoBetween { get; set; }
        public string?[]? AltAccountNoIn { get; set; }

        public string? Address1 { get; set; }

        public string? Address1StartsWith { get; set; }
        public string? Address1EndsWith { get; set; }
        public string? Address1Contains { get; set; }
        public string? Address1Like { get; set; }
        public string?[]? Address1Between { get; set; }
        public string?[]? Address1In { get; set; }

        public string? Address2 { get; set; }

        public string? Address2StartsWith { get; set; }
        public string? Address2EndsWith { get; set; }
        public string? Address2Contains { get; set; }
        public string? Address2Like { get; set; }
        public string?[]? Address2Between { get; set; }
        public string?[]? Address2In { get; set; }

        public string? Address3 { get; set; }

        public string? Address3StartsWith { get; set; }
        public string? Address3EndsWith { get; set; }
        public string? Address3Contains { get; set; }
        public string? Address3Like { get; set; }
        public string?[]? Address3Between { get; set; }
        public string?[]? Address3In { get; set; }

        public string? Address4 { get; set; }

        public string? Address4StartsWith { get; set; }
        public string? Address4EndsWith { get; set; }
        public string? Address4Contains { get; set; }
        public string? Address4Like { get; set; }
        public string?[]? Address4Between { get; set; }
        public string?[]? Address4In { get; set; }

        public string? PostCode { get; set; }

        public string? PostCodeStartsWith { get; set; }
        public string? PostCodeEndsWith { get; set; }
        public string? PostCodeContains { get; set; }
        public string? PostCodeLike { get; set; }
        public string?[]? PostCodeBetween { get; set; }
        public string?[]? PostCodeIn { get; set; }

        public string? Country { get; set; }

        public string? CountryStartsWith { get; set; }
        public string? CountryEndsWith { get; set; }
        public string? CountryContains { get; set; }
        public string? CountryLike { get; set; }
        public string?[]? CountryBetween { get; set; }
        public string?[]? CountryIn { get; set; }

        public string? Phone { get; set; }

        public string? PhoneStartsWith { get; set; }
        public string? PhoneEndsWith { get; set; }
        public string? PhoneContains { get; set; }
        public string? PhoneLike { get; set; }
        public string?[]? PhoneBetween { get; set; }
        public string?[]? PhoneIn { get; set; }

        public bool? AccountOnHold { get; set; }

        public string? EmailAddress { get; set; }

        public string? EmailAddressStartsWith { get; set; }
        public string? EmailAddressEndsWith { get; set; }
        public string? EmailAddressContains { get; set; }
        public string? EmailAddressLike { get; set; }
        public string?[]? EmailAddressBetween { get; set; }
        public string?[]? EmailAddressIn { get; set; }

        public decimal? CurrentBalance { get; set; }

        public decimal? CurrentBalanceGreaterThanOrEqualTo { get; set; }
        public decimal? CurrentBalanceGreaterThan { get; set; }
        public decimal? CurrentBalanceLessThan { get; set; }
        public decimal? CurrentBalanceLessThanOrEqualTo { get; set; }
        public decimal? CurrentBalanceNotEqualTo { get; set; }
        public decimal?[]? CurrentBalanceBetween { get; set; }
        public decimal?[]? CurrentBalanceIn { get; set; }

        public bool? WebAccess { get; set; }

        public DateTimeOffset? LastSavedDateTime { get; set; }

        public DateTimeOffset? LastSavedDateTimeGreaterThanOrEqualTo { get; set; }
        public DateTimeOffset? LastSavedDateTimeGreaterThan { get; set; }
        public DateTimeOffset? LastSavedDateTimeLessThan { get; set; }
        public DateTimeOffset? LastSavedDateTimeLessThanOrEqualTo { get; set; }
        public DateTimeOffset? LastSavedDateTimeNotEqualTo { get; set; }
        public DateTimeOffset?[]? LastSavedDateTimeBetween { get; set; }
        public DateTimeOffset?[]? LastSavedDateTimeIn { get; set; }

        public byte? TradingStatus { get; set; }

        public byte? TradingStatusGreaterThanOrEqualTo { get; set; }
        public byte? TradingStatusGreaterThan { get; set; }
        public byte? TradingStatusLessThan { get; set; }
        public byte? TradingStatusLessThanOrEqualTo { get; set; }
        public byte? TradingStatusNotEqualTo { get; set; }
        public byte?[]? TradingStatusBetween { get; set; }
        public byte?[]? TradingStatusIn { get; set; }

        public string? DebtorClassificationID { get; set; }

        public string? DebtorClassificationIDStartsWith { get; set; }
        public string? DebtorClassificationIDEndsWith { get; set; }
        public string? DebtorClassificationIDContains { get; set; }
        public string? DebtorClassificationIDLike { get; set; }
        public string?[]? DebtorClassificationIDBetween { get; set; }
        public string?[]? DebtorClassificationIDIn { get; set; }

        public string? ClassificationDescription { get; set; }

        public string? ClassificationDescriptionStartsWith { get; set; }
        public string? ClassificationDescriptionEndsWith { get; set; }
        public string? ClassificationDescriptionContains { get; set; }
        public string? ClassificationDescriptionLike { get; set; }
        public string?[]? ClassificationDescriptionBetween { get; set; }
        public string?[]? ClassificationDescriptionIn { get; set; }

        public string? Category1ID { get; set; }

        public string? Category1IDStartsWith { get; set; }
        public string? Category1IDEndsWith { get; set; }
        public string? Category1IDContains { get; set; }
        public string? Category1IDLike { get; set; }
        public string?[]? Category1IDBetween { get; set; }
        public string?[]? Category1IDIn { get; set; }

        public string? Category1Description { get; set; }

        public string? Category1DescriptionStartsWith { get; set; }
        public string? Category1DescriptionEndsWith { get; set; }
        public string? Category1DescriptionContains { get; set; }
        public string? Category1DescriptionLike { get; set; }
        public string?[]? Category1DescriptionBetween { get; set; }
        public string?[]? Category1DescriptionIn { get; set; }

        public string? Category2ID { get; set; }

        public string? Category2IDStartsWith { get; set; }
        public string? Category2IDEndsWith { get; set; }
        public string? Category2IDContains { get; set; }
        public string? Category2IDLike { get; set; }
        public string?[]? Category2IDBetween { get; set; }
        public string?[]? Category2IDIn { get; set; }

        public string? Category2Description { get; set; }

        public string? Category2DescriptionStartsWith { get; set; }
        public string? Category2DescriptionEndsWith { get; set; }
        public string? Category2DescriptionContains { get; set; }
        public string? Category2DescriptionLike { get; set; }
        public string?[]? Category2DescriptionBetween { get; set; }
        public string?[]? Category2DescriptionIn { get; set; }

        public string? Category3ID { get; set; }

        public string? Category3IDStartsWith { get; set; }
        public string? Category3IDEndsWith { get; set; }
        public string? Category3IDContains { get; set; }
        public string? Category3IDLike { get; set; }
        public string?[]? Category3IDBetween { get; set; }
        public string?[]? Category3IDIn { get; set; }

        public string? Category3Description { get; set; }

        public string? Category3DescriptionStartsWith { get; set; }
        public string? Category3DescriptionEndsWith { get; set; }
        public string? Category3DescriptionContains { get; set; }
        public string? Category3DescriptionLike { get; set; }
        public string?[]? Category3DescriptionBetween { get; set; }
        public string?[]? Category3DescriptionIn { get; set; }

        public string? Category4ID { get; set; }

        public string? Category4IDStartsWith { get; set; }
        public string? Category4IDEndsWith { get; set; }
        public string? Category4IDContains { get; set; }
        public string? Category4IDLike { get; set; }
        public string?[]? Category4IDBetween { get; set; }
        public string?[]? Category4IDIn { get; set; }

        public string? Category4Description { get; set; }

        public string? Category4DescriptionStartsWith { get; set; }
        public string? Category4DescriptionEndsWith { get; set; }
        public string? Category4DescriptionContains { get; set; }
        public string? Category4DescriptionLike { get; set; }
        public string?[]? Category4DescriptionBetween { get; set; }
        public string?[]? Category4DescriptionIn { get; set; }

        public string? Category5ID { get; set; }

        public string? Category5IDStartsWith { get; set; }
        public string? Category5IDEndsWith { get; set; }
        public string? Category5IDContains { get; set; }
        public string? Category5IDLike { get; set; }
        public string?[]? Category5IDBetween { get; set; }
        public string?[]? Category5IDIn { get; set; }

        public string? Category5Description { get; set; }

        public string? Category5DescriptionStartsWith { get; set; }
        public string? Category5DescriptionEndsWith { get; set; }
        public string? Category5DescriptionContains { get; set; }
        public string? Category5DescriptionLike { get; set; }
        public string?[]? Category5DescriptionBetween { get; set; }
        public string?[]? Category5DescriptionIn { get; set; }

        public string? PriceSchemeID { get; set; }

        public string? PriceSchemeIDStartsWith { get; set; }
        public string? PriceSchemeIDEndsWith { get; set; }
        public string? PriceSchemeIDContains { get; set; }
        public string? PriceSchemeIDLike { get; set; }
        public string?[]? PriceSchemeIDBetween { get; set; }
        public string?[]? PriceSchemeIDIn { get; set; }

        public string? PriceSchemeDescription { get; set; }

        public string? PriceSchemeDescriptionStartsWith { get; set; }
        public string? PriceSchemeDescriptionEndsWith { get; set; }
        public string? PriceSchemeDescriptionContains { get; set; }
        public string? PriceSchemeDescriptionLike { get; set; }
        public string?[]? PriceSchemeDescriptionBetween { get; set; }
        public string?[]? PriceSchemeDescriptionIn { get; set; }

        public string? PricingGroupDescription { get; set; }

        public string? PricingGroupDescriptionStartsWith { get; set; }
        public string? PricingGroupDescriptionEndsWith { get; set; }
        public string? PricingGroupDescriptionContains { get; set; }
        public string? PricingGroupDescriptionLike { get; set; }
        public string?[]? PricingGroupDescriptionBetween { get; set; }
        public string?[]? PricingGroupDescriptionIn { get; set; }

    }

    public partial class v_Jiwa_Debtor_Transactions_List
    {
        [Required]
        public virtual string? TransID { get; set; }

        [Required]
        public virtual string? DebtorID { get; set; }

        [Required]
        public virtual string? AccountNo { get; set; }

        public virtual string? Name { get; set; }
        public virtual DateTime? TranDate { get; set; }
        public virtual DateTime? DueDate { get; set; }
        public virtual string? InvRemitNo { get; set; }
        [Required]
        public virtual bool DebitCredit { get; set; }

        [Required]
        public virtual decimal Amount { get; set; }

        public virtual decimal? AllocatedAmount { get; set; }
        [Required]
        public virtual decimal GSTAmount { get; set; }

        public virtual decimal? OutstandingAmount { get; set; }
        [Required]
        public virtual decimal DebitAmountExTax { get; set; }

        [Required]
        public virtual decimal CreditAmountExTax { get; set; }

        public virtual decimal? DebitAmountIncTax { get; set; }
        public virtual decimal? CreditAmountIncTax { get; set; }
        public virtual string? Description { get; set; }
        public virtual string? SourceID { get; set; }
        public virtual string? Ref { get; set; }
        public virtual string? Remark { get; set; }
        public virtual string? Note { get; set; }
        [Required]
        public virtual bool AgedOut { get; set; }

        [Required]
        public virtual string? CurrencyID { get; set; }

        [Required]
        public virtual string? CurrencyShortName { get; set; }

        [Required]
        public virtual string? CurrencyName { get; set; }

        [Required]
        public virtual short DecimalPlaces { get; set; }
    }

    [Route("/Queries/DebtorTransactionList", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    public partial class v_Jiwa_Debtor_Transactions_ListQuery
        : QueryDb<v_Jiwa_Debtor_Transactions_List>, IReturn<QueryResponse<v_Jiwa_Debtor_Transactions_List>>
    {
        public virtual string? TransID { get; set; }
        public virtual string? TransIDStartsWith { get; set; }
        public virtual string? TransIDEndsWith { get; set; }
        public virtual string? TransIDContains { get; set; }
        public virtual string? TransIDLike { get; set; }
        public virtual string?[]? TransIDBetween { get; set; }
        public virtual string?[]? TransIDIn { get; set; }
        public virtual string? DebtorID { get; set; }
        public virtual string? DebtorIDStartsWith { get; set; }
        public virtual string? DebtorIDEndsWith { get; set; }
        public virtual string? DebtorIDContains { get; set; }
        public virtual string? DebtorIDLike { get; set; }
        public virtual string?[]? DebtorIDBetween { get; set; }
        public virtual string?[]? DebtorIDIn { get; set; }
        public virtual string? AccountNo { get; set; }
        public virtual string? AccountNoStartsWith { get; set; }
        public virtual string? AccountNoEndsWith { get; set; }
        public virtual string? AccountNoContains { get; set; }
        public virtual string? AccountNoLike { get; set; }
        public virtual string?[]? AccountNoBetween { get; set; }
        public virtual string?[]? AccountNoIn { get; set; }
        public virtual string? Name { get; set; }
        public virtual string? NameStartsWith { get; set; }
        public virtual string? NameEndsWith { get; set; }
        public virtual string? NameContains { get; set; }
        public virtual string? NameLike { get; set; }
        public virtual string?[]? NameBetween { get; set; }
        public virtual string?[]? NameIn { get; set; }
        public virtual DateTime? TranDate { get; set; }
        public virtual DateTime? TranDateGreaterThanOrEqualTo { get; set; }
        public virtual DateTime? TranDateGreaterThan { get; set; }
        public virtual DateTime? TranDateLessThan { get; set; }
        public virtual DateTime? TranDateLessThanOrEqualTo { get; set; }
        public virtual DateTime? TranDateNotEqualTo { get; set; }
        public virtual DateTime?[]? TranDateBetween { get; set; }
        public virtual DateTime?[]? TranDateIn { get; set; }
        public virtual DateTime? DueDate { get; set; }
        public virtual DateTime? DueDateGreaterThanOrEqualTo { get; set; }
        public virtual DateTime? DueDateGreaterThan { get; set; }
        public virtual DateTime? DueDateLessThan { get; set; }
        public virtual DateTime? DueDateLessThanOrEqualTo { get; set; }
        public virtual DateTime? DueDateNotEqualTo { get; set; }
        public virtual DateTime?[]? DueDateBetween { get; set; }
        public virtual DateTime?[]? DueDateIn { get; set; }
        public virtual string? InvRemitNo { get; set; }
        public virtual string? InvRemitNoStartsWith { get; set; }
        public virtual string? InvRemitNoEndsWith { get; set; }
        public virtual string? InvRemitNoContains { get; set; }
        public virtual string? InvRemitNoLike { get; set; }
        public virtual string?[]? InvRemitNoBetween { get; set; }
        public virtual string?[]? InvRemitNoIn { get; set; }
        public virtual bool? DebitCredit { get; set; }
        public virtual decimal? Amount { get; set; }
        public virtual decimal? AmountGreaterThanOrEqualTo { get; set; }
        public virtual decimal? AmountGreaterThan { get; set; }
        public virtual decimal? AmountLessThan { get; set; }
        public virtual decimal? AmountLessThanOrEqualTo { get; set; }
        public virtual decimal? AmountNotEqualTo { get; set; }
        public virtual decimal?[]? AmountBetween { get; set; }
        public virtual decimal?[]? AmountIn { get; set; }
        public virtual decimal? AllocatedAmount { get; set; }
        public virtual decimal? AllocatedAmountGreaterThanOrEqualTo { get; set; }
        public virtual decimal? AllocatedAmountGreaterThan { get; set; }
        public virtual decimal? AllocatedAmountLessThan { get; set; }
        public virtual decimal? AllocatedAmountLessThanOrEqualTo { get; set; }
        public virtual decimal? AllocatedAmountNotEqualTo { get; set; }
        public virtual decimal?[]? AllocatedAmountBetween { get; set; }
        public virtual decimal?[]? AllocatedAmountIn { get; set; }
        public virtual decimal? GSTAmount { get; set; }
        public virtual decimal? GSTAmountGreaterThanOrEqualTo { get; set; }
        public virtual decimal? GSTAmountGreaterThan { get; set; }
        public virtual decimal? GSTAmountLessThan { get; set; }
        public virtual decimal? GSTAmountLessThanOrEqualTo { get; set; }
        public virtual decimal? GSTAmountNotEqualTo { get; set; }
        public virtual decimal?[]? GSTAmountBetween { get; set; }
        public virtual decimal?[]? GSTAmountIn { get; set; }
        public virtual decimal? OutstandingAmount { get; set; }
        public virtual decimal? OutstandingAmountGreaterThanOrEqualTo { get; set; }
        public virtual decimal? OutstandingAmountGreaterThan { get; set; }
        public virtual decimal? OutstandingAmountLessThan { get; set; }
        public virtual decimal? OutstandingAmountLessThanOrEqualTo { get; set; }
        public virtual decimal? OutstandingAmountNotEqualTo { get; set; }
        public virtual decimal?[]? OutstandingAmountBetween { get; set; }
        public virtual decimal?[]? OutstandingAmountIn { get; set; }
        public virtual decimal? DebitAmountExTax { get; set; }
        public virtual decimal? DebitAmountExTaxGreaterThanOrEqualTo { get; set; }
        public virtual decimal? DebitAmountExTaxGreaterThan { get; set; }
        public virtual decimal? DebitAmountExTaxLessThan { get; set; }
        public virtual decimal? DebitAmountExTaxLessThanOrEqualTo { get; set; }
        public virtual decimal? DebitAmountExTaxNotEqualTo { get; set; }
        public virtual decimal?[]? DebitAmountExTaxBetween { get; set; }
        public virtual decimal?[]? DebitAmountExTaxIn { get; set; }
        public virtual decimal? CreditAmountExTax { get; set; }
        public virtual decimal? CreditAmountExTaxGreaterThanOrEqualTo { get; set; }
        public virtual decimal? CreditAmountExTaxGreaterThan { get; set; }
        public virtual decimal? CreditAmountExTaxLessThan { get; set; }
        public virtual decimal? CreditAmountExTaxLessThanOrEqualTo { get; set; }
        public virtual decimal? CreditAmountExTaxNotEqualTo { get; set; }
        public virtual decimal?[]? CreditAmountExTaxBetween { get; set; }
        public virtual decimal?[]? CreditAmountExTaxIn { get; set; }
        public virtual decimal? DebitAmountIncTax { get; set; }
        public virtual decimal? DebitAmountIncTaxGreaterThanOrEqualTo { get; set; }
        public virtual decimal? DebitAmountIncTaxGreaterThan { get; set; }
        public virtual decimal? DebitAmountIncTaxLessThan { get; set; }
        public virtual decimal? DebitAmountIncTaxLessThanOrEqualTo { get; set; }
        public virtual decimal? DebitAmountIncTaxNotEqualTo { get; set; }
        public virtual decimal?[]? DebitAmountIncTaxBetween { get; set; }
        public virtual decimal?[]? DebitAmountIncTaxIn { get; set; }
        public virtual decimal? CreditAmountIncTax { get; set; }
        public virtual decimal? CreditAmountIncTaxGreaterThanOrEqualTo { get; set; }
        public virtual decimal? CreditAmountIncTaxGreaterThan { get; set; }
        public virtual decimal? CreditAmountIncTaxLessThan { get; set; }
        public virtual decimal? CreditAmountIncTaxLessThanOrEqualTo { get; set; }
        public virtual decimal? CreditAmountIncTaxNotEqualTo { get; set; }
        public virtual decimal?[]? CreditAmountIncTaxBetween { get; set; }
        public virtual decimal?[]? CreditAmountIncTaxIn { get; set; }
        public virtual string? Description { get; set; }
        public virtual string? DescriptionStartsWith { get; set; }
        public virtual string? DescriptionEndsWith { get; set; }
        public virtual string? DescriptionContains { get; set; }
        public virtual string? DescriptionLike { get; set; }
        public virtual string?[]? DescriptionBetween { get; set; }
        public virtual string?[]? DescriptionIn { get; set; }
        public virtual string? SourceID { get; set; }
        public virtual string? SourceIDStartsWith { get; set; }
        public virtual string? SourceIDEndsWith { get; set; }
        public virtual string? SourceIDContains { get; set; }
        public virtual string? SourceIDLike { get; set; }
        public virtual string?[]? SourceIDBetween { get; set; }
        public virtual string?[]? SourceIDIn { get; set; }
        public virtual string? Ref { get; set; }
        public virtual string? RefStartsWith { get; set; }
        public virtual string? RefEndsWith { get; set; }
        public virtual string? RefContains { get; set; }
        public virtual string? RefLike { get; set; }
        public virtual string?[]? RefBetween { get; set; }
        public virtual string?[]? RefIn { get; set; }
        public virtual string? Remark { get; set; }
        public virtual string? RemarkStartsWith { get; set; }
        public virtual string? RemarkEndsWith { get; set; }
        public virtual string? RemarkContains { get; set; }
        public virtual string? RemarkLike { get; set; }
        public virtual string?[]? RemarkBetween { get; set; }
        public virtual string?[]? RemarkIn { get; set; }
        public virtual string? Note { get; set; }
        public virtual string? NoteStartsWith { get; set; }
        public virtual string? NoteEndsWith { get; set; }
        public virtual string? NoteContains { get; set; }
        public virtual string? NoteLike { get; set; }
        public virtual string?[]? NoteBetween { get; set; }
        public virtual string?[]? NoteIn { get; set; }
        public virtual bool? AgedOut { get; set; }
        public virtual string? CurrencyID { get; set; }
        public virtual string? CurrencyIDStartsWith { get; set; }
        public virtual string? CurrencyIDEndsWith { get; set; }
        public virtual string? CurrencyIDContains { get; set; }
        public virtual string? CurrencyIDLike { get; set; }
        public virtual string?[]? CurrencyIDBetween { get; set; }
        public virtual string?[]? CurrencyIDIn { get; set; }
        public virtual string? CurrencyShortName { get; set; }
        public virtual string? CurrencyShortNameStartsWith { get; set; }
        public virtual string? CurrencyShortNameEndsWith { get; set; }
        public virtual string? CurrencyShortNameContains { get; set; }
        public virtual string? CurrencyShortNameLike { get; set; }
        public virtual string?[]? CurrencyShortNameBetween { get; set; }
        public virtual string?[]? CurrencyShortNameIn { get; set; }
        public virtual string? CurrencyName { get; set; }
        public virtual string? CurrencyNameStartsWith { get; set; }
        public virtual string? CurrencyNameEndsWith { get; set; }
        public virtual string? CurrencyNameContains { get; set; }
        public virtual string? CurrencyNameLike { get; set; }
        public virtual string?[]? CurrencyNameBetween { get; set; }
        public virtual string?[]? CurrencyNameIn { get; set; }
        public virtual short? DecimalPlaces { get; set; }
        public virtual short? DecimalPlacesGreaterThanOrEqualTo { get; set; }
        public virtual short? DecimalPlacesGreaterThan { get; set; }
        public virtual short? DecimalPlacesLessThan { get; set; }
        public virtual short? DecimalPlacesLessThanOrEqualTo { get; set; }
        public virtual short? DecimalPlacesNotEqualTo { get; set; }
        public virtual short?[]? DecimalPlacesBetween { get; set; }
        public virtual short?[]? DecimalPlacesIn { get; set; }
    }

    [Serializable()]
    public partial class DB_Classification
    {
        [Required]
        [PrimaryKey]
        public string? DebtorClassificationID { get; set; }
        [Required]
        public DateTimeOffset? LastSavedDateTime { get; set; }
        [Required]
        public string? Description { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerIDDebtorControl { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerIDDebtorSales { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerIDDebtorDiscounts { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerIDDebtorSourcedInvoices { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerIDDebtorDebitAdjustment { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerIDDebtorSourcedReceipts { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerIDDebtorCreditAdjustment { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerIDDebtorFreight { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerIDDebtorInsurance { get; set; }
        public short? TermsDays { get; set; }
        [Required]
        public short TermsType { get; set; }
        [References(typeof(IN_PriceSchemes))]
        public string? PriceSchemeID { get; set; }
        [References(typeof(DB_PricingGroups))]
        public string? PricingGroupID { get; set; }
        public string? LedgerIDDebtorRealisedGainLoss { get; set; }
        [Required]
        public bool IsDefault { get; set; }
    }

    [Serializable()]
    public partial class DB_PricingGroups
    {
        [Required]
        [PrimaryKey]
        public string? RecID { get; set; }
        [Required]
        public DateTimeOffset LastSavedDateTime { get; set; }
        [Required]
        public string? Description { get; set; }
        public bool? DefaultPriceGroup { get; set; }
    }

    [Route("/Queries/DB_Classification", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    public partial class DB_ClassificationQuery : QueryDb<DB_Classification>
    {

        public string? DebtorClassificationID { get; set; }
        public string? DebtorClassificationIDStartsWith { get; set; }
        public string? DebtorClassificationIDEndsWith { get; set; }
        public string? DebtorClassificationIDContains { get; set; }
        public string? DebtorClassificationIDLike { get; set; }
        public string?[]? DebtorClassificationIDBetween { get; set; }
        public string?[]? DebtorClassificationIDIn { get; set; }

        public DateTimeOffset? LastSavedDateTime { get; set; }

        public DateTimeOffset? LastSavedDateTimeGreaterThanOrEqualTo { get; set; }
        public DateTimeOffset? LastSavedDateTimeGreaterThan { get; set; }
        public DateTimeOffset? LastSavedDateTimeLessThan { get; set; }
        public DateTimeOffset? LastSavedDateTimeLessThanOrEqualTo { get; set; }
        public DateTimeOffset? LastSavedDateTimeNotEqualTo { get; set; }
        public DateTimeOffset?[]? LastSavedDateTimeBetween { get; set; }
        public DateTimeOffset?[]? LastSavedDateTimeIn { get; set; }

        public string? Description { get; set; }

        public string? DescriptionStartsWith { get; set; }
        public string? DescriptionEndsWith { get; set; }
        public string? DescriptionContains { get; set; }
        public string? DescriptionLike { get; set; }
        public string?[]? DescriptionBetween { get; set; }
        public string?[]? DescriptionIn { get; set; }

        public string? LedgerIDDebtorControl { get; set; }

        public string? LedgerIDDebtorControlStartsWith { get; set; }
        public string? LedgerIDDebtorControlEndsWith { get; set; }
        public string? LedgerIDDebtorControlContains { get; set; }
        public string? LedgerIDDebtorControlLike { get; set; }
        public string?[]? LedgerIDDebtorControlBetween { get; set; }
        public string?[]? LedgerIDDebtorControlIn { get; set; }

        public string? LedgerIDDebtorSales { get; set; }

        public string? LedgerIDDebtorSalesStartsWith { get; set; }
        public string? LedgerIDDebtorSalesEndsWith { get; set; }
        public string? LedgerIDDebtorSalesContains { get; set; }
        public string? LedgerIDDebtorSalesLike { get; set; }
        public string?[]? LedgerIDDebtorSalesBetween { get; set; }
        public string?[]? LedgerIDDebtorSalesIn { get; set; }
        public string? LedgerIDDebtorDiscounts { get; set; }

        public string? LedgerIDDebtorDiscountsStartsWith { get; set; }
        public string? LedgerIDDebtorDiscountsEndsWith { get; set; }
        public string? LedgerIDDebtorDiscountsContains { get; set; }
        public string? LedgerIDDebtorDiscountsLike { get; set; }
        public string?[]? LedgerIDDebtorDiscountsBetween { get; set; }
        public string?[]? LedgerIDDebtorDiscountsIn { get; set; }

        public string? LedgerIDDebtorSourcedInvoices { get; set; }

        public string? LedgerIDDebtorSourcedInvoicesStartsWith { get; set; }
        public string? LedgerIDDebtorSourcedInvoicesEndsWith { get; set; }
        public string? LedgerIDDebtorSourcedInvoicesContains { get; set; }
        public string? LedgerIDDebtorSourcedInvoicesLike { get; set; }
        public string?[]? LedgerIDDebtorSourcedInvoicesBetween { get; set; }
        public string?[]? LedgerIDDebtorSourcedInvoicesIn { get; set; } = null;
        public string? LedgerIDDebtorDebitAdjustment { get; set; }

        public string? LedgerIDDebtorDebitAdjustmentStartsWith { get; set; }
        public string? LedgerIDDebtorDebitAdjustmentEndsWith { get; set; }
        public string? LedgerIDDebtorDebitAdjustmentContains { get; set; }
        public string? LedgerIDDebtorDebitAdjustmentLike { get; set; }
        public string?[]? LedgerIDDebtorDebitAdjustmentBetween { get; set; }
        public string?[]? LedgerIDDebtorDebitAdjustmentIn { get; set; }

        public string? LedgerIDDebtorSourcedReceipts { get; set; }

        public string? LedgerIDDebtorSourcedReceiptsStartsWith { get; set; }
        public string? LedgerIDDebtorSourcedReceiptsEndsWith { get; set; }
        public string? LedgerIDDebtorSourcedReceiptsContains { get; set; }
        public string? LedgerIDDebtorSourcedReceiptsLike { get; set; }
        public string?[]? LedgerIDDebtorSourcedReceiptsBetween { get; set; }
        public string?[]? LedgerIDDebtorSourcedReceiptsIn { get; set; }
        public string? LedgerIDDebtorCreditAdjustment { get; set; }

        public string? LedgerIDDebtorCreditAdjustmentStartsWith { get; set; }
        public string? LedgerIDDebtorCreditAdjustmentEndsWith { get; set; }
        public string? LedgerIDDebtorCreditAdjustmentContains { get; set; }
        public string? LedgerIDDebtorCreditAdjustmentLike { get; set; }
        public string?[]? LedgerIDDebtorCreditAdjustmentBetween { get; set; }
        public string?[]? LedgerIDDebtorCreditAdjustmentIn { get; set; }

        public string? LedgerIDDebtorFreight { get; set; }

        public string? LedgerIDDebtorFreightStartsWith { get; set; }
        public string? LedgerIDDebtorFreightEndsWith { get; set; }
        public string? LedgerIDDebtorFreightContains { get; set; }
        public string? LedgerIDDebtorFreightLike { get; set; }
        public string?[]? LedgerIDDebtorFreightBetween { get; set; }
        public string?[]? LedgerIDDebtorFreightIn { get; set; }
        public string? LedgerIDDebtorInsurance { get; set; }

        public string? LedgerIDDebtorInsuranceStartsWith { get; set; }
        public string? LedgerIDDebtorInsuranceEndsWith { get; set; }
        public string? LedgerIDDebtorInsuranceContains { get; set; }
        public string? LedgerIDDebtorInsuranceLike { get; set; }
        public string?[]? LedgerIDDebtorInsuranceBetween { get; set; }
        public string?[]? LedgerIDDebtorInsuranceIn { get; set; }

        public short? TermsDays { get; set; }

        public short? TermsDaysGreaterThanOrEqualTo { get; set; }
        public short? TermsDaysGreaterThan { get; set; }
        public short? TermsDaysLessThan { get; set; }
        public short? TermsDaysLessThanOrEqualTo { get; set; }
        public short? TermsDaysNotEqualTo { get; set; }
        public short?[]? TermsDaysBetween { get; set; }
        public short?[]? TermsDaysIn { get; set; }

        public short? TermsType { get; set; }

        public short? TermsTypeGreaterThanOrEqualTo { get; set; }
        public short? TermsTypeGreaterThan { get; set; }
        public short? TermsTypeLessThan { get; set; }
        public short? TermsTypeLessThanOrEqualTo { get; set; }
        public short? TermsTypeNotEqualTo { get; set; }
        public short?[]? TermsTypeBetween { get; set; }
        public short?[]? TermsTypeIn { get; set; }

        public string? PriceSchemeID { get; set; }

        public string? PriceSchemeIDStartsWith { get; set; }
        public string? PriceSchemeIDEndsWith { get; set; }
        public string? PriceSchemeIDContains { get; set; }
        public string? PriceSchemeIDLike { get; set; }
        public string?[]? PriceSchemeIDBetween { get; set; }
        public string?[]? PriceSchemeIDIn { get; set; }

        public string? PricingGroupID { get; set; }

        public string? PricingGroupIDStartsWith { get; set; }
        public string? PricingGroupIDEndsWith { get; set; }
        public string? PricingGroupIDContains { get; set; }
        public string? PricingGroupIDLike { get; set; }
        public string?[]? PricingGroupIDBetween { get; set; }
        public string?[]? PricingGroupIDIn { get; set; } = null;
        public string? LedgerIDDebtorRealisedGainLoss { get; set; }

        public string? LedgerIDDebtorRealisedGainLossStartsWith { get; set; }
        public string? LedgerIDDebtorRealisedGainLossEndsWith { get; set; }
        public string? LedgerIDDebtorRealisedGainLossContains { get; set; }
        public string? LedgerIDDebtorRealisedGainLossLike { get; set; }
        public string?[]? LedgerIDDebtorRealisedGainLossBetween { get; set; }
        public string?[]? LedgerIDDebtorRealisedGainLossIn { get; set; }

        public bool? IsDefault { get; set; }

    }

    [Serializable()]
    public partial class DB_Categories
    {
        [Required]
        public int CategoryNo { get; set; }
        [Required]
        public string? CategoryID { get; set; }
        public string? Description { get; set; }
        [Required]
        public bool DefaultCategory { get; set; }
        [Required]
        public DateTimeOffset LastSavedDateTime { get; set; }
    }

    [Route("/Queries/DB_Categories", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    public partial class DB_CategoriesQuery : QueryDb<DB_Categories>
    {
        public int? CategoryNo { get; set; }

        public int? CategoryNoGreaterThanOrEqualTo { get; set; }
        public int? CategoryNoGreaterThan { get; set; }
        public int? CategoryNoLessThan { get; set; }
        public int? CategoryNoLessThanOrEqualTo { get; set; }
        public int? CategoryNoNotEqualTo { get; set; }
        public int[]? CategoryNoBetween { get; set; }
        public int[]? CategoryNoIn { get; set; }

        public string? CategoryID { get; set; }

        public string? CategoryIDStartsWith { get; set; }
        public string? CategoryIDEndsWith { get; set; }
        public string? CategoryIDContains { get; set; }
        public string? CategoryIDLike { get; set; }
        public string[]? CategoryIDBetween { get; set; }
        public string[]? CategoryIDIn { get; set; }

        public string? Description { get; set; }

        public string? DescriptionStartsWith { get; set; }
        public string? DescriptionEndsWith { get; set; }
        public string? DescriptionContains { get; set; }
        public string? DescriptionLike { get; set; }
        public string[]? DescriptionBetween { get; set; }
        public string[]? DescriptionIn { get; set; } = null;
        public bool? DefaultCategory { get; set; }

        public DateTimeOffset? LastSavedDateTime { get; set; }

        public DateTimeOffset? LastSavedDateTimeGreaterThanOrEqualTo { get; set; }
        public DateTimeOffset? LastSavedDateTimeGreaterThan { get; set; }
        public DateTimeOffset? LastSavedDateTimeLessThan { get; set; }
        public DateTimeOffset? LastSavedDateTimeLessThanOrEqualTo { get; set; }
        public DateTimeOffset? LastSavedDateTimeNotEqualTo { get; set; }
        public DateTimeOffset?[]? LastSavedDateTimeBetween { get; set; }
        public DateTimeOffset?[]? LastSavedDateTimeIn { get; set; }

    }
    #endregion

    #region "General Ledger"
    [Serializable()]
    public partial class GL_Ledger
    {
        [Required]
        [PrimaryKey]
        public string? GLLedgerID { get; set; }
        [Required]
        public DateTimeOffset? LastSavedDateTime { get; set; }
        [References(typeof(GL_Category))]
        [Required]
        public string? GLCategoryID { get; set; }
        [Required]
        public string? AccountNo { get; set; }
        public string? Seg1 { get; set; }
        public string? Seg2 { get; set; }
        public string? Seg3 { get; set; }
        public string? Seg4 { get; set; }
        public string? Seg5 { get; set; }
        public string? Seg6 { get; set; }
        public string? Description { get; set; }
        public decimal? LastYearOpen { get; set; }
        public decimal? CurrYearOpen { get; set; }
        public decimal? CurrBal { get; set; }
        public byte? ExpSign { get; set; }
        [Required]
        public short AccClass { get; set; }
        public bool? DistributionAcc { get; set; }
        [Required]
        public string? ShortCut { get; set; }
        [Required]
        public short PostingAcc { get; set; }
        public string? ParentAccNo { get; set; }
        public bool? UseTransCode1 { get; set; }
        public bool? UseTransCode2 { get; set; }
        public bool? UseTransCode3 { get; set; }
        public bool? UseStaffCode { get; set; }
        public string? Details { get; set; }
        public bool? IsEnabled { get; set; }
    }

    [Serializable()]
    public partial class GL_Category
    {
        [Required]
        [PrimaryKey]
        public string? GLCategoryID { get; set; }
        [Required]
        public DateTimeOffset? LastSavedDateTime { get; set; }
        public string? Description { get; set; }
        public byte? ExpSign { get; set; }
        public byte? AccType { get; set; }
        public string? Group1 { get; set; }
        public string? Group2 { get; set; }
        public int? Group2DisplayOrder { get; set; }
        public int? Group1DisplayOrder { get; set; }
    }
    #endregion

    #region "Inventory"
    public partial class v_Jiwa_Inventory_Item_List
    {
        public v_Jiwa_Inventory_Item_List()
        {
            Picture = new byte[] { };
        }

        [Required]
        public virtual string? InventoryID { get; set; }

        [Required]
        public virtual string? PartNo { get; set; }

        public virtual string? Description { get; set; }
        public virtual byte[] Picture { get; set; }
        [Required]
        public virtual string? InventoryClassificationID { get; set; }

        public virtual string? ClassificationDescription { get; set; }
        [Required]
        public virtual string? Category1ID { get; set; }

        public virtual string? Category1Description { get; set; }
        [Required]
        public virtual string? Category2ID { get; set; }

        public virtual string? Category2Description { get; set; }
        [Required]
        public virtual string? Category3ID { get; set; }

        public virtual string? Category3Description { get; set; }
        [Required]
        public virtual string? Category4ID { get; set; }

        public virtual string? Category4Description { get; set; }
        [Required]
        public virtual string? Category5ID { get; set; }

        public virtual string? Category5Description { get; set; }
        [Required]
        public virtual string? IN_LogicalID { get; set; }

        public virtual string? LogicalWarehouseDescription { get; set; }
        [Required]
        public virtual string? IN_PhysicalID { get; set; }

        [Required]
        public virtual string? PhysicalWarehouseDescription { get; set; }

        public virtual decimal? AvailableStock { get; set; }
        public virtual decimal? SellPrice { get; set; }
        public virtual decimal? RRPPrice { get; set; }
        [Required]
        public virtual DateTimeOffset LastSavedDateTime { get; set; }

        public virtual decimal? InStock { get; set; }
    }

    [Route("/Queries/InventoryItemList", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    public partial class v_Jiwa_Inventory_Item_ListQuery
        : QueryDb<v_Jiwa_Inventory_Item_List>, IReturn<QueryResponse<v_Jiwa_Inventory_Item_List>>
    {
        public virtual string? InventoryID { get; set; }
        public virtual string? InventoryIDStartsWith { get; set; }
        public virtual string? InventoryIDEndsWith { get; set; }
        public virtual string? InventoryIDContains { get; set; }
        public virtual string? InventoryIDLike { get; set; }
        public virtual string[]? InventoryIDBetween { get; set; }
        public virtual string[]? InventoryIDIn { get; set; }
        public virtual string? PartNo { get; set; }
        public virtual string? PartNoStartsWith { get; set; }
        public virtual string? PartNoEndsWith { get; set; }
        public virtual string? PartNoContains { get; set; }
        public virtual string? PartNoLike { get; set; }
        public virtual string[]? PartNoBetween { get; set; }
        public virtual string[]? PartNoIn { get; set; }
        public virtual string? Description { get; set; }
        public virtual string? DescriptionStartsWith { get; set; }
        public virtual string? DescriptionEndsWith { get; set; }
        public virtual string? DescriptionContains { get; set; }
        public virtual string? DescriptionLike { get; set; }
        public virtual string[]? DescriptionBetween { get; set; }
        public virtual string[]? DescriptionIn { get; set; }
        public virtual byte[]? Picture { get; set; }
        public virtual string? InventoryClassificationID { get; set; }
        public virtual string? InventoryClassificationIDStartsWith { get; set; }
        public virtual string? InventoryClassificationIDEndsWith { get; set; }
        public virtual string? InventoryClassificationIDContains { get; set; }
        public virtual string? InventoryClassificationIDLike { get; set; }
        public virtual string[]? InventoryClassificationIDBetween { get; set; }
        public virtual string[]? InventoryClassificationIDIn { get; set; }
        public virtual string? ClassificationDescription { get; set; }
        public virtual string? ClassificationDescriptionStartsWith { get; set; }
        public virtual string? ClassificationDescriptionEndsWith { get; set; }
        public virtual string? ClassificationDescriptionContains { get; set; }
        public virtual string? ClassificationDescriptionLike { get; set; }
        public virtual string[]? ClassificationDescriptionBetween { get; set; }
        public virtual string[]? ClassificationDescriptionIn { get; set; }
        public virtual string? Category1ID { get; set; }
        public virtual string? Category1IDStartsWith { get; set; }
        public virtual string? Category1IDEndsWith { get; set; }
        public virtual string? Category1IDContains { get; set; }
        public virtual string? Category1IDLike { get; set; }
        public virtual string[]? Category1IDBetween { get; set; }
        public virtual string[]? Category1IDIn { get; set; }
        public virtual string? Category1Description { get; set; }
        public virtual string? Category1DescriptionStartsWith { get; set; }
        public virtual string? Category1DescriptionEndsWith { get; set; }
        public virtual string? Category1DescriptionContains { get; set; }
        public virtual string? Category1DescriptionLike { get; set; }
        public virtual string[]? Category1DescriptionBetween { get; set; }
        public virtual string[]? Category1DescriptionIn { get; set; }
        public virtual string? Category2ID { get; set; }
        public virtual string? Category2IDStartsWith { get; set; }
        public virtual string? Category2IDEndsWith { get; set; }
        public virtual string? Category2IDContains { get; set; }
        public virtual string? Category2IDLike { get; set; }
        public virtual string[]? Category2IDBetween { get; set; }
        public virtual string[]? Category2IDIn { get; set; }
        public virtual string? Category2Description { get; set; }
        public virtual string? Category2DescriptionStartsWith { get; set; }
        public virtual string? Category2DescriptionEndsWith { get; set; }
        public virtual string? Category2DescriptionContains { get; set; }
        public virtual string? Category2DescriptionLike { get; set; }
        public virtual string[]? Category2DescriptionBetween { get; set; }
        public virtual string[]? Category2DescriptionIn { get; set; }
        public virtual string? Category3ID { get; set; }
        public virtual string? Category3IDStartsWith { get; set; }
        public virtual string? Category3IDEndsWith { get; set; }
        public virtual string? Category3IDContains { get; set; }
        public virtual string? Category3IDLike { get; set; }
        public virtual string[]? Category3IDBetween { get; set; }
        public virtual string[]? Category3IDIn { get; set; }
        public virtual string? Category3Description { get; set; }
        public virtual string? Category3DescriptionStartsWith { get; set; }
        public virtual string? Category3DescriptionEndsWith { get; set; }
        public virtual string? Category3DescriptionContains { get; set; }
        public virtual string? Category3DescriptionLike { get; set; }
        public virtual string[]? Category3DescriptionBetween { get; set; }
        public virtual string[]? Category3DescriptionIn { get; set; }
        public virtual string? Category4ID { get; set; }
        public virtual string? Category4IDStartsWith { get; set; }
        public virtual string? Category4IDEndsWith { get; set; }
        public virtual string? Category4IDContains { get; set; }
        public virtual string? Category4IDLike { get; set; }
        public virtual string[]? Category4IDBetween { get; set; }
        public virtual string[]? Category4IDIn { get; set; }
        public virtual string? Category4Description { get; set; }
        public virtual string? Category4DescriptionStartsWith { get; set; }
        public virtual string? Category4DescriptionEndsWith { get; set; }
        public virtual string? Category4DescriptionContains { get; set; }
        public virtual string? Category4DescriptionLike { get; set; }
        public virtual string[]? Category4DescriptionBetween { get; set; }
        public virtual string[]? Category4DescriptionIn { get; set; }
        public virtual string? Category5ID { get; set; }
        public virtual string? Category5IDStartsWith { get; set; }
        public virtual string? Category5IDEndsWith { get; set; }
        public virtual string? Category5IDContains { get; set; }
        public virtual string? Category5IDLike { get; set; }
        public virtual string[]? Category5IDBetween { get; set; }
        public virtual string[]? Category5IDIn { get; set; }
        public virtual string? Category5Description { get; set; }
        public virtual string? Category5DescriptionStartsWith { get; set; }
        public virtual string? Category5DescriptionEndsWith { get; set; }
        public virtual string? Category5DescriptionContains { get; set; }
        public virtual string? Category5DescriptionLike { get; set; }
        public virtual string[]? Category5DescriptionBetween { get; set; }
        public virtual string[]? Category5DescriptionIn { get; set; }
        public virtual string? IN_LogicalID { get; set; }
        public virtual string? IN_LogicalIDStartsWith { get; set; }
        public virtual string? IN_LogicalIDEndsWith { get; set; }
        public virtual string? IN_LogicalIDContains { get; set; }
        public virtual string? IN_LogicalIDLike { get; set; }
        public virtual string[]? IN_LogicalIDBetween { get; set; }
        public virtual string[]? IN_LogicalIDIn { get; set; }
        public virtual string? LogicalWarehouseDescription { get; set; }
        public virtual string? LogicalWarehouseDescriptionStartsWith { get; set; }
        public virtual string? LogicalWarehouseDescriptionEndsWith { get; set; }
        public virtual string? LogicalWarehouseDescriptionContains { get; set; }
        public virtual string? LogicalWarehouseDescriptionLike { get; set; }
        public virtual string[]? LogicalWarehouseDescriptionBetween { get; set; }
        public virtual string[]? LogicalWarehouseDescriptionIn { get; set; }
        public virtual string? IN_PhysicalID { get; set; }
        public virtual string? IN_PhysicalIDStartsWith { get; set; }
        public virtual string? IN_PhysicalIDEndsWith { get; set; }
        public virtual string? IN_PhysicalIDContains { get; set; }
        public virtual string? IN_PhysicalIDLike { get; set; }
        public virtual string[]? IN_PhysicalIDBetween { get; set; }
        public virtual string[]? IN_PhysicalIDIn { get; set; }
        public virtual string? PhysicalWarehouseDescription { get; set; }
        public virtual string? PhysicalWarehouseDescriptionStartsWith { get; set; }
        public virtual string? PhysicalWarehouseDescriptionEndsWith { get; set; }
        public virtual string? PhysicalWarehouseDescriptionContains { get; set; }
        public virtual string? PhysicalWarehouseDescriptionLike { get; set; }
        public virtual string[]? PhysicalWarehouseDescriptionBetween { get; set; }
        public virtual string[]? PhysicalWarehouseDescriptionIn { get; set; }
        public virtual decimal? AvailableStock { get; set; }
        public virtual decimal? AvailableStockGreaterThanOrEqualTo { get; set; }
        public virtual decimal? AvailableStockGreaterThan { get; set; }
        public virtual decimal? AvailableStockLessThan { get; set; }
        public virtual decimal? AvailableStockLessThanOrEqualTo { get; set; }
        public virtual decimal? AvailableStockNotEqualTo { get; set; }
        public virtual decimal?[]? AvailableStockBetween { get; set; }
        public virtual decimal?[]? AvailableStockIn { get; set; }
        public virtual decimal? SellPrice { get; set; }
        public virtual decimal? SellPriceGreaterThanOrEqualTo { get; set; }
        public virtual decimal? SellPriceGreaterThan { get; set; }
        public virtual decimal? SellPriceLessThan { get; set; }
        public virtual decimal? SellPriceLessThanOrEqualTo { get; set; }
        public virtual decimal? SellPriceNotEqualTo { get; set; }
        public virtual decimal?[]? SellPriceBetween { get; set; }
        public virtual decimal?[]? SellPriceIn { get; set; }
        public virtual decimal? RRPPrice { get; set; }
        public virtual decimal? RRPPriceGreaterThanOrEqualTo { get; set; }
        public virtual decimal? RRPPriceGreaterThan { get; set; }
        public virtual decimal? RRPPriceLessThan { get; set; }
        public virtual decimal? RRPPriceLessThanOrEqualTo { get; set; }
        public virtual decimal? RRPPriceNotEqualTo { get; set; }
        public virtual decimal?[]? RRPPriceBetween { get; set; }
        public virtual decimal?[]? RRPPriceIn { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeGreaterThanOrEqualTo { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeGreaterThan { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeLessThan { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeLessThanOrEqualTo { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeNotEqualTo { get; set; }
        public virtual DateTimeOffset[]? LastSavedDateTimeBetween { get; set; }
        public virtual DateTimeOffset[]? LastSavedDateTimeIn { get; set; }
        public virtual decimal? InStock { get; set; }
        public virtual decimal? InStockGreaterThanOrEqualTo { get; set; }
        public virtual decimal? InStockGreaterThan { get; set; }
        public virtual decimal? InStockLessThan { get; set; }
        public virtual decimal? InStockLessThanOrEqualTo { get; set; }
        public virtual decimal? InStockNotEqualTo { get; set; }
        public virtual decimal?[]? InStockBetween { get; set; }
        public virtual decimal?[]? InStockIn { get; set; }
    }

    public partial class v_IN_SOHWithBinLocations
    {
        [Required]
        public virtual string? LinkID { get; set; }

        [Required]
        public virtual DateTimeOffset LastSavedDateTime { get; set; }

        [Required]
        public virtual string? InventoryID { get; set; }

        public virtual DateTime? DateIn { get; set; }
        public virtual decimal? QuantityIn { get; set; }
        public virtual decimal? LCostIn { get; set; }
        public virtual decimal? SCostIn { get; set; }
        public virtual decimal? SpecialPrice { get; set; }
        public virtual decimal? QuantityLeft { get; set; }
        public virtual string? SerialNo { get; set; }
        public virtual decimal? TaxPaid { get; set; }
        public virtual string? Ref { get; set; }
        public virtual string? SourceID { get; set; }
        public virtual string? HistoryText { get; set; }
        public virtual decimal? QuantityAllocated { get; set; }
        [Required]
        public virtual string? IN_LogicalID { get; set; }

        public virtual DateTime? ExpiryDate { get; set; }
        public virtual string? INBinLookupID { get; set; }
        public virtual string? Description { get; set; }
        public virtual string? ShortName { get; set; }
    }

    [Route("/Queries/INSOHWithBinLocations", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    public partial class v_IN_SOHWithBinLocationsQuery
        : QueryDb<v_IN_SOHWithBinLocations>, IReturn<QueryResponse<v_IN_SOHWithBinLocations>>
    {
        public virtual string? LinkID { get; set; }
        public virtual string? LinkIDStartsWith { get; set; }
        public virtual string? LinkIDEndsWith { get; set; }
        public virtual string? LinkIDContains { get; set; }
        public virtual string? LinkIDLike { get; set; }
        public virtual string[]? LinkIDBetween { get; set; }
        public virtual string[]? LinkIDIn { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeGreaterThanOrEqualTo { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeGreaterThan { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeLessThan { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeLessThanOrEqualTo { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeNotEqualTo { get; set; }
        public virtual DateTimeOffset[]? LastSavedDateTimeBetween { get; set; }
        public virtual DateTimeOffset[]? LastSavedDateTimeIn { get; set; }
        public virtual string? InventoryID { get; set; }
        public virtual string? InventoryIDStartsWith { get; set; }
        public virtual string? InventoryIDEndsWith { get; set; }
        public virtual string? InventoryIDContains { get; set; }
        public virtual string? InventoryIDLike { get; set; }
        public virtual string[]? InventoryIDBetween { get; set; }
        public virtual string[]? InventoryIDIn { get; set; }
        public virtual DateTime? DateIn { get; set; }
        public virtual DateTime? DateInGreaterThanOrEqualTo { get; set; }
        public virtual DateTime? DateInGreaterThan { get; set; }
        public virtual DateTime? DateInLessThan { get; set; }
        public virtual DateTime? DateInLessThanOrEqualTo { get; set; }
        public virtual DateTime? DateInNotEqualTo { get; set; }
        public virtual DateTime?[]? DateInBetween { get; set; }
        public virtual DateTime?[]? DateInIn { get; set; }
        public virtual decimal? QuantityIn { get; set; }
        public virtual decimal? QuantityInGreaterThanOrEqualTo { get; set; }
        public virtual decimal? QuantityInGreaterThan { get; set; }
        public virtual decimal? QuantityInLessThan { get; set; }
        public virtual decimal? QuantityInLessThanOrEqualTo { get; set; }
        public virtual decimal? QuantityInNotEqualTo { get; set; }
        public virtual decimal?[]? QuantityInBetween { get; set; }
        public virtual decimal?[]? QuantityInIn { get; set; }
        public virtual decimal? LCostIn { get; set; }
        public virtual decimal? LCostInGreaterThanOrEqualTo { get; set; }
        public virtual decimal? LCostInGreaterThan { get; set; }
        public virtual decimal? LCostInLessThan { get; set; }
        public virtual decimal? LCostInLessThanOrEqualTo { get; set; }
        public virtual decimal? LCostInNotEqualTo { get; set; }
        public virtual decimal?[]? LCostInBetween { get; set; }
        public virtual decimal?[]? LCostInIn { get; set; }
        public virtual decimal? SCostIn { get; set; }
        public virtual decimal? SCostInGreaterThanOrEqualTo { get; set; }
        public virtual decimal? SCostInGreaterThan { get; set; }
        public virtual decimal? SCostInLessThan { get; set; }
        public virtual decimal? SCostInLessThanOrEqualTo { get; set; }
        public virtual decimal? SCostInNotEqualTo { get; set; }
        public virtual decimal?[]? SCostInBetween { get; set; }
        public virtual decimal?[]? SCostInIn { get; set; }
        public virtual decimal? SpecialPrice { get; set; }
        public virtual decimal? SpecialPriceGreaterThanOrEqualTo { get; set; }
        public virtual decimal? SpecialPriceGreaterThan { get; set; }
        public virtual decimal? SpecialPriceLessThan { get; set; }
        public virtual decimal? SpecialPriceLessThanOrEqualTo { get; set; }
        public virtual decimal? SpecialPriceNotEqualTo { get; set; }
        public virtual decimal?[]? SpecialPriceBetween { get; set; }
        public virtual decimal?[]? SpecialPriceIn { get; set; }
        public virtual decimal? QuantityLeft { get; set; }
        public virtual decimal? QuantityLeftGreaterThanOrEqualTo { get; set; }
        public virtual decimal? QuantityLeftGreaterThan { get; set; }
        public virtual decimal? QuantityLeftLessThan { get; set; }
        public virtual decimal? QuantityLeftLessThanOrEqualTo { get; set; }
        public virtual decimal? QuantityLeftNotEqualTo { get; set; }
        public virtual decimal?[]? QuantityLeftBetween { get; set; }
        public virtual decimal?[]? QuantityLeftIn { get; set; }
        public virtual string? SerialNo { get; set; }
        public virtual string? SerialNoStartsWith { get; set; }
        public virtual string? SerialNoEndsWith { get; set; }
        public virtual string? SerialNoContains { get; set; }
        public virtual string? SerialNoLike { get; set; }
        public virtual string[]? SerialNoBetween { get; set; }
        public virtual string[]? SerialNoIn { get; set; }
        public virtual decimal? TaxPaid { get; set; }
        public virtual decimal? TaxPaidGreaterThanOrEqualTo { get; set; }
        public virtual decimal? TaxPaidGreaterThan { get; set; }
        public virtual decimal? TaxPaidLessThan { get; set; }
        public virtual decimal? TaxPaidLessThanOrEqualTo { get; set; }
        public virtual decimal? TaxPaidNotEqualTo { get; set; }
        public virtual decimal?[]? TaxPaidBetween { get; set; }
        public virtual decimal?[]? TaxPaidIn { get; set; }
        public virtual string? Ref { get; set; }
        public virtual string? RefStartsWith { get; set; }
        public virtual string? RefEndsWith { get; set; }
        public virtual string? RefContains { get; set; }
        public virtual string? RefLike { get; set; }
        public virtual string[]? RefBetween { get; set; }
        public virtual string[]? RefIn { get; set; }
        public virtual string? SourceID { get; set; }
        public virtual string? SourceIDStartsWith { get; set; }
        public virtual string? SourceIDEndsWith { get; set; }
        public virtual string? SourceIDContains { get; set; }
        public virtual string? SourceIDLike { get; set; }
        public virtual string[]? SourceIDBetween { get; set; }
        public virtual string[]? SourceIDIn { get; set; }
        public virtual string? HistoryText { get; set; }
        public virtual string? HistoryTextStartsWith { get; set; }
        public virtual string? HistoryTextEndsWith { get; set; }
        public virtual string? HistoryTextContains { get; set; }
        public virtual string? HistoryTextLike { get; set; }
        public virtual string[]? HistoryTextBetween { get; set; }
        public virtual string[]? HistoryTextIn { get; set; }
        public virtual decimal? QuantityAllocated { get; set; }
        public virtual decimal? QuantityAllocatedGreaterThanOrEqualTo { get; set; }
        public virtual decimal? QuantityAllocatedGreaterThan { get; set; }
        public virtual decimal? QuantityAllocatedLessThan { get; set; }
        public virtual decimal? QuantityAllocatedLessThanOrEqualTo { get; set; }
        public virtual decimal? QuantityAllocatedNotEqualTo { get; set; }
        public virtual decimal?[]? QuantityAllocatedBetween { get; set; }
        public virtual decimal?[]? QuantityAllocatedIn { get; set; }
        public virtual string? IN_LogicalID { get; set; }
        public virtual string? IN_LogicalIDStartsWith { get; set; }
        public virtual string? IN_LogicalIDEndsWith { get; set; }
        public virtual string? IN_LogicalIDContains { get; set; }
        public virtual string? IN_LogicalIDLike { get; set; }
        public virtual string[]? IN_LogicalIDBetween { get; set; }
        public virtual string[]? IN_LogicalIDIn { get; set; }
        public virtual DateTime? ExpiryDate { get; set; }
        public virtual DateTime? ExpiryDateGreaterThanOrEqualTo { get; set; }
        public virtual DateTime? ExpiryDateGreaterThan { get; set; }
        public virtual DateTime? ExpiryDateLessThan { get; set; }
        public virtual DateTime? ExpiryDateLessThanOrEqualTo { get; set; }
        public virtual DateTime? ExpiryDateNotEqualTo { get; set; }
        public virtual DateTime?[]? ExpiryDateBetween { get; set; }
        public virtual DateTime?[]? ExpiryDateIn { get; set; }
        public virtual string? INBinLookupID { get; set; }
        public virtual string? INBinLookupIDStartsWith { get; set; }
        public virtual string? INBinLookupIDEndsWith { get; set; }
        public virtual string? INBinLookupIDContains { get; set; }
        public virtual string? INBinLookupIDLike { get; set; }
        public virtual string[]? INBinLookupIDBetween { get; set; }
        public virtual string[]? INBinLookupIDIn { get; set; }
        public virtual string? Description { get; set; }
        public virtual string? DescriptionStartsWith { get; set; }
        public virtual string? DescriptionEndsWith { get; set; }
        public virtual string? DescriptionContains { get; set; }
        public virtual string? DescriptionLike { get; set; }
        public virtual string[]? DescriptionBetween { get; set; }
        public virtual string[]? DescriptionIn { get; set; }
        public virtual string? ShortName { get; set; }
        public virtual string? ShortNameStartsWith { get; set; }
        public virtual string? ShortNameEndsWith { get; set; }
        public virtual string? ShortNameContains { get; set; }
        public virtual string? ShortNameLike { get; set; }
        public virtual string[]? ShortNameBetween { get; set; }
        public virtual string[]? ShortNameIn { get; set; }
    }

    [Serializable()]
    public partial class IN_Classification
    {
        [Required]
        [PrimaryKey]
        public string? InventoryClassificationID { get; set; }
        public string? Description { get; set; }
        [Required]
        public DateTimeOffset LastSavedDateTime { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerInvValue { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerMovement_COG { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerExpAsset { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerExpLiab { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerDelAsset { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerDelLiab { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerAssignedValue { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerCogVariance { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerInvSales { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerAccumulator { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerPurchases { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerShipComplete { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerWriteOn { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerWriteOff { get; set; }
        [References(typeof(GL_Ledger))]
        public string? LedgerCostPriceAdj { get; set; }
        public string? GSTInwardsID { get; set; }
        public string? GSTOutwardsID { get; set; }
        public string? GSTAdjustmentsINID { get; set; }
        public string? GSTAdjustmentsOUTID { get; set; }
        [Required]
        public bool WebEnabled { get; set; }
        public bool? DefaultClassification { get; set; }
        public string? PricingGroupID { get; set; }
        public byte[]? Picture { get; set; }
    }


    [Route("/Queries/IN_Classification", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    public partial class IN_ClassificationQuery : QueryDb<IN_Classification>
    {

        public string? InventoryClassificationID { get; set; }
        public string? InventoryClassificationIDStartsWith { get; set; }
        public string? InventoryClassificationIDEndsWith { get; set; }
        public string? InventoryClassificationIDContains { get; set; }
        public string? InventoryClassificationIDLike { get; set; }
        public string?[]? InventoryClassificationIDBetween { get; set; }
        public string?[]? InventoryClassificationIDIn { get; set; }

        public string? Description { get; set; }

        public string? DescriptionStartsWith { get; set; }
        public string? DescriptionEndsWith { get; set; }
        public string? DescriptionContains { get; set; }
        public string? DescriptionLike { get; set; }
        public string[]? DescriptionBetween { get; set; }
        public string[]? DescriptionIn { get; set; }

        public DateTimeOffset? LastSavedDateTime { get; set; }

        public DateTimeOffset? LastSavedDateTimeGreaterThanOrEqualTo { get; set; }
        public DateTimeOffset? LastSavedDateTimeGreaterThan { get; set; }
        public DateTimeOffset? LastSavedDateTimeLessThan { get; set; }
        public DateTimeOffset? LastSavedDateTimeLessThanOrEqualTo { get; set; }
        public DateTimeOffset? LastSavedDateTimeNotEqualTo { get; set; }
        public DateTimeOffset?[]? LastSavedDateTimeBetween { get; set; }
        public DateTimeOffset?[]? LastSavedDateTimeIn { get; set; }

        public string? LedgerInvValue { get; set; }

        public string? LedgerInvValueStartsWith { get; set; }
        public string? LedgerInvValueEndsWith { get; set; }
        public string? LedgerInvValueContains { get; set; }
        public string? LedgerInvValueLike { get; set; }
        public string?[]? LedgerInvValueBetween { get; set; }
        public string?[]? LedgerInvValueIn { get; set; }

        public string? LedgerMovement_COG { get; set; }

        public string? LedgerMovement_COGStartsWith { get; set; }
        public string? LedgerMovement_COGEndsWith { get; set; }
        public string? LedgerMovement_COGContains { get; set; }
        public string? LedgerMovement_COGLike { get; set; }
        public string?[]? LedgerMovement_COGBetween { get; set; }
        public string?[]? LedgerMovement_COGIn { get; set; } = null;
        public string? LedgerExpAsset { get; set; }

        public string? LedgerExpAssetStartsWith { get; set; }
        public string? LedgerExpAssetEndsWith { get; set; }
        public string? LedgerExpAssetContains { get; set; }
        public string? LedgerExpAssetLike { get; set; }
        public string?[]? LedgerExpAssetBetween { get; set; }
        public string?[]? LedgerExpAssetIn { get; set; }

        public string? LedgerExpLiab { get; set; }

        public string? LedgerExpLiabStartsWith { get; set; }
        public string? LedgerExpLiabEndsWith { get; set; }
        public string? LedgerExpLiabContains { get; set; }
        public string? LedgerExpLiabLike { get; set; }
        public string?[]? LedgerExpLiabBetween { get; set; }
        public string?[]? LedgerExpLiabIn { get; set; }
        public string? LedgerDelAsset { get; set; }

        public string? LedgerDelAssetStartsWith { get; set; }
        public string? LedgerDelAssetEndsWith { get; set; }
        public string? LedgerDelAssetContains { get; set; }
        public string? LedgerDelAssetLike { get; set; }
        public string?[]? LedgerDelAssetBetween { get; set; }
        public string?[]? LedgerDelAssetIn { get; set; }

        public string? LedgerDelLiab { get; set; }

        public string? LedgerDelLiabStartsWith { get; set; }
        public string? LedgerDelLiabEndsWith { get; set; }
        public string? LedgerDelLiabContains { get; set; }
        public string? LedgerDelLiabLike { get; set; }
        public string?[]? LedgerDelLiabBetween { get; set; }
        public string?[]? LedgerDelLiabIn { get; set; } = null;
        public string? LedgerAssignedValue { get; set; }

        public string? LedgerAssignedValueStartsWith { get; set; }
        public string? LedgerAssignedValueEndsWith { get; set; }
        public string? LedgerAssignedValueContains { get; set; }
        public string? LedgerAssignedValueLike { get; set; }
        public string?[]? LedgerAssignedValueBetween { get; set; }
        public string?[]? LedgerAssignedValueIn { get; set; }

        public string? LedgerCogVariance { get; set; }

        public string? LedgerCogVarianceStartsWith { get; set; }
        public string? LedgerCogVarianceEndsWith { get; set; }
        public string? LedgerCogVarianceContains { get; set; }
        public string? LedgerCogVarianceLike { get; set; }
        public string?[]? LedgerCogVarianceBetween { get; set; }
        public string?[]? LedgerCogVarianceIn { get; set; }
        public string? LedgerInvSales { get; set; }

        public string? LedgerInvSalesStartsWith { get; set; }
        public string? LedgerInvSalesEndsWith { get; set; }
        public string? LedgerInvSalesContains { get; set; }
        public string? LedgerInvSalesLike { get; set; }
        public string?[]? LedgerInvSalesBetween { get; set; }
        public string?[]? LedgerInvSalesIn { get; set; }

        public string? LedgerAccumulator { get; set; }

        public string? LedgerAccumulatorStartsWith { get; set; }
        public string? LedgerAccumulatorEndsWith { get; set; }
        public string? LedgerAccumulatorContains { get; set; }
        public string? LedgerAccumulatorLike { get; set; }
        public string?[]? LedgerAccumulatorBetween { get; set; }
        public string?[]? LedgerAccumulatorIn { get; set; } = null;
        public string? LedgerPurchases { get; set; }

        public string? LedgerPurchasesStartsWith { get; set; }
        public string? LedgerPurchasesEndsWith { get; set; }
        public string? LedgerPurchasesContains { get; set; }
        public string? LedgerPurchasesLike { get; set; }
        public string?[]? LedgerPurchasesBetween { get; set; }
        public string?[]? LedgerPurchasesIn { get; set; } = null;

        public string? LedgerShipComplete { get; set; }

        public string? LedgerShipCompleteStartsWith { get; set; }
        public string? LedgerShipCompleteEndsWith { get; set; }
        public string? LedgerShipCompleteContains { get; set; }
        public string? LedgerShipCompleteLike { get; set; }
        public string?[]? LedgerShipCompleteBetween { get; set; }
        public string?[]? LedgerShipCompleteIn { get; set; } = null;
        public string? LedgerWriteOn { get; set; }

        public string? LedgerWriteOnStartsWith { get; set; }
        public string? LedgerWriteOnEndsWith { get; set; }
        public string? LedgerWriteOnContains { get; set; }
        public string? LedgerWriteOnLike { get; set; }
        public string?[]? LedgerWriteOnBetween { get; set; }
        public string?[]? LedgerWriteOnIn { get; set; } = null;

        public string? LedgerWriteOff { get; set; }

        public string? LedgerWriteOffStartsWith { get; set; }
        public string? LedgerWriteOffEndsWith { get; set; }
        public string? LedgerWriteOffContains { get; set; }
        public string? LedgerWriteOffLike { get; set; }
        public string?[]? LedgerWriteOffBetween { get; set; }
        public string?[]? LedgerWriteOffIn { get; set; } = null;
        public string? LedgerCostPriceAdj { get; set; }

        public string? LedgerCostPriceAdjStartsWith { get; set; }
        public string? LedgerCostPriceAdjEndsWith { get; set; }
        public string? LedgerCostPriceAdjContains { get; set; }
        public string? LedgerCostPriceAdjLike { get; set; }
        public string?[]? LedgerCostPriceAdjBetween { get; set; }
        public string?[]? LedgerCostPriceAdjIn { get; set; } = null;

        public string? GSTInwardsID { get; set; }

        public string? GSTInwardsIDStartsWith { get; set; }
        public string? GSTInwardsIDEndsWith { get; set; }
        public string? GSTInwardsIDContains { get; set; }
        public string? GSTInwardsIDLike { get; set; }
        public string?[]? GSTInwardsIDBetween { get; set; }
        public string?[]? GSTInwardsIDIn { get; set; } = null;
        public string? GSTOutwardsID { get; set; }

        public string? GSTOutwardsIDStartsWith { get; set; }
        public string? GSTOutwardsIDEndsWith { get; set; }
        public string? GSTOutwardsIDContains { get; set; }
        public string? GSTOutwardsIDLike { get; set; }
        public string?[]? GSTOutwardsIDBetween { get; set; }
        public string?[]? GSTOutwardsIDIn { get; set; } = null;

        public string? GSTAdjustmentsINID { get; set; }

        public string? GSTAdjustmentsINIDStartsWith { get; set; }
        public string? GSTAdjustmentsINIDEndsWith { get; set; }
        public string? GSTAdjustmentsINIDContains { get; set; }
        public string? GSTAdjustmentsINIDLike { get; set; }
        public string?[]? GSTAdjustmentsINIDBetween { get; set; }
        public string?[]? GSTAdjustmentsINIDIn { get; set; } = null;
        public string? GSTAdjustmentsOUTID { get; set; }

        public string? GSTAdjustmentsOUTIDStartsWith { get; set; }
        public string? GSTAdjustmentsOUTIDEndsWith { get; set; }
        public string? GSTAdjustmentsOUTIDContains { get; set; }
        public string? GSTAdjustmentsOUTIDLike { get; set; }
        public string?[]? GSTAdjustmentsOUTIDBetween { get; set; }
        public string?[]? GSTAdjustmentsOUTIDIn { get; set; } = null;

        public bool? WebEnabled { get; set; }

        public bool? DefaultClassification { get; set; }

        public string? PricingGroupID { get; set; }

        public string? PricingGroupIDStartsWith { get; set; }
        public string? PricingGroupIDEndsWith { get; set; }
        public string? PricingGroupIDContains { get; set; }
        public string? PricingGroupIDLike { get; set; }
        public string?[]? PricingGroupIDBetween { get; set; }
        public string?[]? PricingGroupIDIn { get; set; } = null;

        public byte[]? Picture { get; set; }
    }

    [Serializable()]
    public partial class IN_Categories
    {
        [Required]
        public int CategoryNo { get; set; }
        [Required]
        public string? CategoryID { get; set; }
        public string? Description { get; set; }
        [Required]
        public bool DefaultCategory { get; set; }
        [Required]
        public DateTimeOffset LastSavedDateTime { get; set; }
        public byte[]? Picture { get; set; }
    }


    [Route("/Queries/IN_Categories", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    public partial class IN_CategoriesQuery : QueryDb<IN_Categories>
    {
        public int? CategoryNo { get; set; }

        public int? CategoryNoGreaterThanOrEqualTo { get; set; }
        public int? CategoryNoGreaterThan { get; set; }
        public int? CategoryNoLessThan { get; set; }
        public int? CategoryNoLessThanOrEqualTo { get; set; }
        public int? CategoryNoNotEqualTo { get; set; }
        public int?[]? CategoryNoBetween { get; set; }
        public int?[]? CategoryNoIn { get; set; }

        public string? CategoryID { get; set; }

        public string? CategoryIDStartsWith { get; set; }
        public string? CategoryIDEndsWith { get; set; }
        public string? CategoryIDContains { get; set; }
        public string? CategoryIDLike { get; set; }
        public string?[]? CategoryIDBetween { get; set; }
        public string?[]? CategoryIDIn { get; set; } = null;
        public string? Description { get; set; }

        public string? DescriptionStartsWith { get; set; }
        public string? DescriptionEndsWith { get; set; }
        public string? DescriptionContains { get; set; }
        public string? DescriptionLike { get; set; }
        public string?[]? DescriptionBetween { get; set; }
        public string?[]? DescriptionIn { get; set; } = null;

        public bool? DefaultCategory { get; set; }

        public DateTimeOffset? LastSavedDateTime { get; set; }

        public DateTimeOffset? LastSavedDateTimeGreaterThanOrEqualTo { get; set; }
        public DateTimeOffset? LastSavedDateTimeGreaterThan { get; set; }
        public DateTimeOffset? LastSavedDateTimeLessThan { get; set; }
        public DateTimeOffset? LastSavedDateTimeLessThanOrEqualTo { get; set; }
        public DateTimeOffset? LastSavedDateTimeNotEqualTo { get; set; }
        public DateTimeOffset?[]? LastSavedDateTimeBetween { get; set; }
        public DateTimeOffset?[]? LastSavedDateTimeIn { get; set; } = null;

        public byte[]? Picture { get; set; }

    }
    #endregion

    #region "Sales Orders"
    public partial class v_Jiwa_SalesOrder_List
    {
        [Required]
        public virtual string? InvoiceID { get; set; }

        [Required]
        public virtual string? InvoiceNo { get; set; }

        public virtual string? InvoiceNoDashHistoryNo { get; set; }
        public virtual string? OrderNo { get; set; }
        public virtual string? SOReference { get; set; }
        [Required]
        public virtual DateTime InvoiceInitDate { get; set; }

        public virtual short? Status { get; set; }
        [Required]
        public virtual bool CreditNote { get; set; }

        [Required]
        public virtual DateTimeOffset LastSavedDateTime { get; set; }

        public virtual decimal? InvoiceTotal { get; set; }
        [Required]
        public virtual string? DebtorID { get; set; }

        [Required]
        public virtual string? AccountNo { get; set; }

        public virtual string? DebtorName { get; set; }
        [Required]
        public virtual string? IN_LogicalID { get; set; }

        public virtual string? LogicalWarehouseDescription { get; set; }
        [Required]
        public virtual string? IN_PhysicalID { get; set; }

        [Required]
        public virtual string? PhysicalWarehouseDescription { get; set; }

        [Required]
        public virtual string? BranchID { get; set; }

        [Required]
        public virtual string? BranchDescription { get; set; }

        public virtual string? CashSaleAddress1 { get; set; }
        public virtual string? CashSaleAddress2 { get; set; }
        public virtual string? CashSaleAddress3 { get; set; }
        public virtual string? CashSaleAddress4 { get; set; }
        public virtual string? CashSalePostcode { get; set; }
        public virtual string? CashSaleCompany { get; set; }
        public virtual string? CashSaleName { get; set; }
        public virtual string? CashSalePhone { get; set; }
        [Required]
        public virtual string? InvoiceHistoryID { get; set; }

        public virtual string? DeliveryAddressContactName { get; set; }
        [Required]
        public virtual string? DeliveryAddressee { get; set; }

        public virtual string? DeliveryAddress1 { get; set; }
        public virtual string? DeliveryAddress2 { get; set; }
        public virtual string? DeliveryAddress3 { get; set; }
        public virtual string? DeliveryAddress4 { get; set; }
        public virtual string? DeliveryAddressPostcode { get; set; }
        [Required]
        public virtual string? DeliveryAddressCountry { get; set; }

        [Required]
        public virtual bool Delivered { get; set; }

        public virtual DateTime? DeliveredDate { get; set; }
        public virtual string? ConsignmentNote { get; set; }
        public virtual decimal? CartageCharge1 { get; set; }
        public virtual decimal? Cartage1TaxAmount { get; set; }
        public virtual decimal? CartageCharge2 { get; set; }
        public virtual decimal? Cartage2TaxAmount { get; set; }
        public virtual decimal? CartageCharge3 { get; set; }
        public virtual decimal? Cartage3TaxAmount { get; set; }
        public virtual string? CourierDetails { get; set; }
        public virtual string? Notes { get; set; }
        public virtual string? EmailAddress { get; set; }
        [Required]
        public virtual string? StaffID { get; set; }

        public virtual string? StaffTitle { get; set; }
        public virtual string? StaffFirstName { get; set; }
        public virtual string? StaffSurname { get; set; }
        [Required]
        public virtual string? StaffUsername { get; set; }

        public virtual byte? HistoryStatus { get; set; }
        public virtual short? HistoryNo { get; set; }
        [Required]
        public virtual string? CurrencyID { get; set; }

        public virtual string? CurrencyShortName { get; set; }
        public virtual string? CurrencyName { get; set; }
        public virtual short? DecimalPlaces { get; set; }
        public virtual decimal? TotalAllocated { get; set; }
        public virtual DateTime? DueDate { get; set; }
    }

    [Route("/Queries/SalesOrderList", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    public partial class v_Jiwa_SalesOrder_ListQuery
        : QueryDb<v_Jiwa_SalesOrder_List>, IReturn<QueryResponse<v_Jiwa_SalesOrder_List>>
    {
        public virtual string? InvoiceID { get; set; }
        public virtual string? InvoiceIDStartsWith { get; set; }
        public virtual string? InvoiceIDEndsWith { get; set; }
        public virtual string? InvoiceIDContains { get; set; }
        public virtual string? InvoiceIDLike { get; set; }
        public virtual string?[]? InvoiceIDBetween { get; set; }
        public virtual string?[]? InvoiceIDIn { get; set; }
        public virtual string? InvoiceNo { get; set; }
        public virtual string? InvoiceNoStartsWith { get; set; }
        public virtual string? InvoiceNoEndsWith { get; set; }
        public virtual string? InvoiceNoContains { get; set; }
        public virtual string? InvoiceNoLike { get; set; }
        public virtual string?[]? InvoiceNoBetween { get; set; }
        public virtual string?[]? InvoiceNoIn { get; set; }
        public virtual string? InvoiceNoDashHistoryNo { get; set; }
        public virtual string? InvoiceNoDashHistoryNoStartsWith { get; set; }
        public virtual string? InvoiceNoDashHistoryNoEndsWith { get; set; }
        public virtual string? InvoiceNoDashHistoryNoContains { get; set; }
        public virtual string? InvoiceNoDashHistoryNoLike { get; set; }
        public virtual string?[]? InvoiceNoDashHistoryNoBetween { get; set; }
        public virtual string?[]? InvoiceNoDashHistoryNoIn { get; set; }
        public virtual string? OrderNo { get; set; }
        public virtual string? OrderNoStartsWith { get; set; }
        public virtual string? OrderNoEndsWith { get; set; }
        public virtual string? OrderNoContains { get; set; }
        public virtual string? OrderNoLike { get; set; }
        public virtual string?[]? OrderNoBetween { get; set; }
        public virtual string?[]? OrderNoIn { get; set; }
        public virtual string? SOReference { get; set; }
        public virtual string? SOReferenceStartsWith { get; set; }
        public virtual string? SOReferenceEndsWith { get; set; }
        public virtual string? SOReferenceContains { get; set; }
        public virtual string? SOReferenceLike { get; set; }
        public virtual string?[]? SOReferenceBetween { get; set; }
        public virtual string?[]? SOReferenceIn { get; set; }
        public virtual DateTime? InvoiceInitDate { get; set; }
        public virtual DateTime? InvoiceInitDateGreaterThanOrEqualTo { get; set; }
        public virtual DateTime? InvoiceInitDateGreaterThan { get; set; }
        public virtual DateTime? InvoiceInitDateLessThan { get; set; }
        public virtual DateTime? InvoiceInitDateLessThanOrEqualTo { get; set; }
        public virtual DateTime? InvoiceInitDateNotEqualTo { get; set; }
        public virtual DateTime?[]? InvoiceInitDateBetween { get; set; }
        public virtual DateTime?[]? InvoiceInitDateIn { get; set; }
        public virtual short? Status { get; set; }
        public virtual short? StatusGreaterThanOrEqualTo { get; set; }
        public virtual short? StatusGreaterThan { get; set; }
        public virtual short? StatusLessThan { get; set; }
        public virtual short? StatusLessThanOrEqualTo { get; set; }
        public virtual short? StatusNotEqualTo { get; set; }
        public virtual short?[]? StatusBetween { get; set; }
        public virtual short?[]? StatusIn { get; set; }
        public virtual bool? CreditNote { get; set; }
        public virtual DateTimeOffset? LastSavedDateTime { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeGreaterThanOrEqualTo { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeGreaterThan { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeLessThan { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeLessThanOrEqualTo { get; set; }
        public virtual DateTimeOffset? LastSavedDateTimeNotEqualTo { get; set; }
        public virtual DateTimeOffset?[]? LastSavedDateTimeBetween { get; set; }
        public virtual DateTimeOffset?[]? LastSavedDateTimeIn { get; set; }
        public virtual decimal? InvoiceTotal { get; set; }
        public virtual decimal? InvoiceTotalGreaterThanOrEqualTo { get; set; }
        public virtual decimal? InvoiceTotalGreaterThan { get; set; }
        public virtual decimal? InvoiceTotalLessThan { get; set; }
        public virtual decimal? InvoiceTotalLessThanOrEqualTo { get; set; }
        public virtual decimal? InvoiceTotalNotEqualTo { get; set; }
        public virtual decimal?[]? InvoiceTotalBetween { get; set; }
        public virtual decimal?[]? InvoiceTotalIn { get; set; }
        public virtual string? DebtorID { get; set; }
        public virtual string? DebtorIDStartsWith { get; set; }
        public virtual string? DebtorIDEndsWith { get; set; }
        public virtual string? DebtorIDContains { get; set; }
        public virtual string? DebtorIDLike { get; set; }
        public virtual string?[]? DebtorIDBetween { get; set; }
        public virtual string?[]? DebtorIDIn { get; set; }
        public virtual string? AccountNo { get; set; }
        public virtual string? AccountNoStartsWith { get; set; }
        public virtual string? AccountNoEndsWith { get; set; }
        public virtual string? AccountNoContains { get; set; }
        public virtual string? AccountNoLike { get; set; }
        public virtual string?[]? AccountNoBetween { get; set; }
        public virtual string?[]? AccountNoIn { get; set; }
        public virtual string? DebtorName { get; set; }
        public virtual string? DebtorNameStartsWith { get; set; }
        public virtual string? DebtorNameEndsWith { get; set; }
        public virtual string? DebtorNameContains { get; set; }
        public virtual string? DebtorNameLike { get; set; }
        public virtual string?[]? DebtorNameBetween { get; set; }
        public virtual string?[]? DebtorNameIn { get; set; }
        public virtual string? IN_LogicalID { get; set; }
        public virtual string? IN_LogicalIDStartsWith { get; set; }
        public virtual string? IN_LogicalIDEndsWith { get; set; }
        public virtual string? IN_LogicalIDContains { get; set; }
        public virtual string? IN_LogicalIDLike { get; set; }
        public virtual string?[]? IN_LogicalIDBetween { get; set; }
        public virtual string?[]? IN_LogicalIDIn { get; set; }
        public virtual string? LogicalWarehouseDescription { get; set; }
        public virtual string? LogicalWarehouseDescriptionStartsWith { get; set; }
        public virtual string? LogicalWarehouseDescriptionEndsWith { get; set; }
        public virtual string? LogicalWarehouseDescriptionContains { get; set; }
        public virtual string? LogicalWarehouseDescriptionLike { get; set; }
        public virtual string?[]? LogicalWarehouseDescriptionBetween { get; set; }
        public virtual string?[]? LogicalWarehouseDescriptionIn { get; set; }
        public virtual string? IN_PhysicalID { get; set; }
        public virtual string? IN_PhysicalIDStartsWith { get; set; }
        public virtual string? IN_PhysicalIDEndsWith { get; set; }
        public virtual string? IN_PhysicalIDContains { get; set; }
        public virtual string? IN_PhysicalIDLike { get; set; }
        public virtual string?[]? IN_PhysicalIDBetween { get; set; }
        public virtual string?[]? IN_PhysicalIDIn { get; set; }
        public virtual string? PhysicalWarehouseDescription { get; set; }
        public virtual string? PhysicalWarehouseDescriptionStartsWith { get; set; }
        public virtual string? PhysicalWarehouseDescriptionEndsWith { get; set; }
        public virtual string? PhysicalWarehouseDescriptionContains { get; set; }
        public virtual string? PhysicalWarehouseDescriptionLike { get; set; }
        public virtual string?[]? PhysicalWarehouseDescriptionBetween { get; set; }
        public virtual string?[]? PhysicalWarehouseDescriptionIn { get; set; }
        public virtual string? BranchID { get; set; }
        public virtual string? BranchIDStartsWith { get; set; }
        public virtual string? BranchIDEndsWith { get; set; }
        public virtual string? BranchIDContains { get; set; }
        public virtual string? BranchIDLike { get; set; }
        public virtual string?[]? BranchIDBetween { get; set; }
        public virtual string?[]? BranchIDIn { get; set; }
        public virtual string? BranchDescription { get; set; }
        public virtual string? BranchDescriptionStartsWith { get; set; }
        public virtual string? BranchDescriptionEndsWith { get; set; }
        public virtual string? BranchDescriptionContains { get; set; }
        public virtual string? BranchDescriptionLike { get; set; }
        public virtual string?[]? BranchDescriptionBetween { get; set; }
        public virtual string?[]? BranchDescriptionIn { get; set; }
        public virtual string? CashSaleAddress1 { get; set; }
        public virtual string? CashSaleAddress1StartsWith { get; set; }
        public virtual string? CashSaleAddress1EndsWith { get; set; }
        public virtual string? CashSaleAddress1Contains { get; set; }
        public virtual string? CashSaleAddress1Like { get; set; }
        public virtual string?[]? CashSaleAddress1Between { get; set; }
        public virtual string?[]? CashSaleAddress1In { get; set; }
        public virtual string? CashSaleAddress2 { get; set; }
        public virtual string? CashSaleAddress2StartsWith { get; set; }
        public virtual string? CashSaleAddress2EndsWith { get; set; }
        public virtual string? CashSaleAddress2Contains { get; set; }
        public virtual string? CashSaleAddress2Like { get; set; }
        public virtual string?[]? CashSaleAddress2Between { get; set; }
        public virtual string?[]? CashSaleAddress2In { get; set; }
        public virtual string? CashSaleAddress3 { get; set; }
        public virtual string? CashSaleAddress3StartsWith { get; set; }
        public virtual string? CashSaleAddress3EndsWith { get; set; }
        public virtual string? CashSaleAddress3Contains { get; set; }
        public virtual string? CashSaleAddress3Like { get; set; }
        public virtual string?[]? CashSaleAddress3Between { get; set; }
        public virtual string?[]? CashSaleAddress3In { get; set; }
        public virtual string? CashSaleAddress4 { get; set; }
        public virtual string? CashSaleAddress4StartsWith { get; set; }
        public virtual string? CashSaleAddress4EndsWith { get; set; }
        public virtual string? CashSaleAddress4Contains { get; set; }
        public virtual string? CashSaleAddress4Like { get; set; }
        public virtual string?[]? CashSaleAddress4Between { get; set; }
        public virtual string?[]? CashSaleAddress4In { get; set; }
        public virtual string? CashSalePostcode { get; set; }
        public virtual string? CashSalePostcodeStartsWith { get; set; }
        public virtual string? CashSalePostcodeEndsWith { get; set; }
        public virtual string? CashSalePostcodeContains { get; set; }
        public virtual string? CashSalePostcodeLike { get; set; }
        public virtual string?[]? CashSalePostcodeBetween { get; set; }
        public virtual string?[]? CashSalePostcodeIn { get; set; }
        public virtual string? CashSaleCompany { get; set; }
        public virtual string? CashSaleCompanyStartsWith { get; set; }
        public virtual string? CashSaleCompanyEndsWith { get; set; }
        public virtual string? CashSaleCompanyContains { get; set; }
        public virtual string? CashSaleCompanyLike { get; set; }
        public virtual string?[]? CashSaleCompanyBetween { get; set; }
        public virtual string?[]? CashSaleCompanyIn { get; set; }
        public virtual string? CashSaleName { get; set; }
        public virtual string? CashSaleNameStartsWith { get; set; }
        public virtual string? CashSaleNameEndsWith { get; set; }
        public virtual string? CashSaleNameContains { get; set; }
        public virtual string? CashSaleNameLike { get; set; }
        public virtual string?[]? CashSaleNameBetween { get; set; }
        public virtual string?[]? CashSaleNameIn { get; set; }
        public virtual string? CashSalePhone { get; set; }
        public virtual string? CashSalePhoneStartsWith { get; set; }
        public virtual string? CashSalePhoneEndsWith { get; set; }
        public virtual string? CashSalePhoneContains { get; set; }
        public virtual string? CashSalePhoneLike { get; set; }
        public virtual string?[]? CashSalePhoneBetween { get; set; }
        public virtual string?[]? CashSalePhoneIn { get; set; }
        public virtual string? InvoiceHistoryID { get; set; }
        public virtual string? InvoiceHistoryIDStartsWith { get; set; }
        public virtual string? InvoiceHistoryIDEndsWith { get; set; }
        public virtual string? InvoiceHistoryIDContains { get; set; }
        public virtual string? InvoiceHistoryIDLike { get; set; }
        public virtual string?[]? InvoiceHistoryIDBetween { get; set; }
        public virtual string?[]? InvoiceHistoryIDIn { get; set; }
        public virtual string? DeliveryAddressContactName { get; set; }
        public virtual string? DeliveryAddressContactNameStartsWith { get; set; }
        public virtual string? DeliveryAddressContactNameEndsWith { get; set; }
        public virtual string? DeliveryAddressContactNameContains { get; set; }
        public virtual string? DeliveryAddressContactNameLike { get; set; }
        public virtual string?[]? DeliveryAddressContactNameBetween { get; set; }
        public virtual string?[]? DeliveryAddressContactNameIn { get; set; }
        public virtual string? DeliveryAddressee { get; set; }
        public virtual string? DeliveryAddresseeStartsWith { get; set; }
        public virtual string? DeliveryAddresseeEndsWith { get; set; }
        public virtual string? DeliveryAddresseeContains { get; set; }
        public virtual string? DeliveryAddresseeLike { get; set; }
        public virtual string?[]? DeliveryAddresseeBetween { get; set; }
        public virtual string?[]? DeliveryAddresseeIn { get; set; }
        public virtual string? DeliveryAddress1 { get; set; }
        public virtual string? DeliveryAddress1StartsWith { get; set; }
        public virtual string? DeliveryAddress1EndsWith { get; set; }
        public virtual string? DeliveryAddress1Contains { get; set; }
        public virtual string? DeliveryAddress1Like { get; set; }
        public virtual string?[]? DeliveryAddress1Between { get; set; }
        public virtual string?[]? DeliveryAddress1In { get; set; }
        public virtual string? DeliveryAddress2 { get; set; }
        public virtual string? DeliveryAddress2StartsWith { get; set; }
        public virtual string? DeliveryAddress2EndsWith { get; set; }
        public virtual string? DeliveryAddress2Contains { get; set; }
        public virtual string? DeliveryAddress2Like { get; set; }
        public virtual string?[]? DeliveryAddress2Between { get; set; }
        public virtual string?[]? DeliveryAddress2In { get; set; }
        public virtual string? DeliveryAddress3 { get; set; }
        public virtual string? DeliveryAddress3StartsWith { get; set; }
        public virtual string? DeliveryAddress3EndsWith { get; set; }
        public virtual string? DeliveryAddress3Contains { get; set; }
        public virtual string? DeliveryAddress3Like { get; set; }
        public virtual string?[]? DeliveryAddress3Between { get; set; }
        public virtual string?[]? DeliveryAddress3In { get; set; }
        public virtual string? DeliveryAddress4 { get; set; }
        public virtual string? DeliveryAddress4StartsWith { get; set; }
        public virtual string? DeliveryAddress4EndsWith { get; set; }
        public virtual string? DeliveryAddress4Contains { get; set; }
        public virtual string? DeliveryAddress4Like { get; set; }
        public virtual string?[]? DeliveryAddress4Between { get; set; }
        public virtual string?[]? DeliveryAddress4In { get; set; }
        public virtual string? DeliveryAddressPostcode { get; set; }
        public virtual string? DeliveryAddressPostcodeStartsWith { get; set; }
        public virtual string? DeliveryAddressPostcodeEndsWith { get; set; }
        public virtual string? DeliveryAddressPostcodeContains { get; set; }
        public virtual string? DeliveryAddressPostcodeLike { get; set; }
        public virtual string?[]? DeliveryAddressPostcodeBetween { get; set; }
        public virtual string?[]? DeliveryAddressPostcodeIn { get; set; }
        public virtual string? DeliveryAddressCountry { get; set; }
        public virtual string? DeliveryAddressCountryStartsWith { get; set; }
        public virtual string? DeliveryAddressCountryEndsWith { get; set; }
        public virtual string? DeliveryAddressCountryContains { get; set; }
        public virtual string? DeliveryAddressCountryLike { get; set; }
        public virtual string?[]? DeliveryAddressCountryBetween { get; set; }
        public virtual string?[]? DeliveryAddressCountryIn { get; set; }
        public virtual bool? Delivered { get; set; }
        public virtual DateTime? DeliveredDate { get; set; }
        public virtual DateTime? DeliveredDateGreaterThanOrEqualTo { get; set; }
        public virtual DateTime? DeliveredDateGreaterThan { get; set; }
        public virtual DateTime? DeliveredDateLessThan { get; set; }
        public virtual DateTime? DeliveredDateLessThanOrEqualTo { get; set; }
        public virtual DateTime? DeliveredDateNotEqualTo { get; set; }
        public virtual DateTime?[]? DeliveredDateBetween { get; set; }
        public virtual DateTime?[]? DeliveredDateIn { get; set; }
        public virtual string? ConsignmentNote { get; set; }
        public virtual string? ConsignmentNoteStartsWith { get; set; }
        public virtual string? ConsignmentNoteEndsWith { get; set; }
        public virtual string? ConsignmentNoteContains { get; set; }
        public virtual string? ConsignmentNoteLike { get; set; }
        public virtual string?[]? ConsignmentNoteBetween { get; set; }
        public virtual string?[]? ConsignmentNoteIn { get; set; }
        public virtual decimal? CartageCharge1 { get; set; }
        public virtual decimal? CartageCharge1GreaterThanOrEqualTo { get; set; }
        public virtual decimal? CartageCharge1GreaterThan { get; set; }
        public virtual decimal? CartageCharge1LessThan { get; set; }
        public virtual decimal? CartageCharge1LessThanOrEqualTo { get; set; }
        public virtual decimal? CartageCharge1NotEqualTo { get; set; }
        public virtual decimal?[]? CartageCharge1Between { get; set; }
        public virtual decimal?[]? CartageCharge1In { get; set; }
        public virtual decimal? Cartage1TaxAmount { get; set; }
        public virtual decimal? Cartage1TaxAmountGreaterThanOrEqualTo { get; set; }
        public virtual decimal? Cartage1TaxAmountGreaterThan { get; set; }
        public virtual decimal? Cartage1TaxAmountLessThan { get; set; }
        public virtual decimal? Cartage1TaxAmountLessThanOrEqualTo { get; set; }
        public virtual decimal? Cartage1TaxAmountNotEqualTo { get; set; }
        public virtual decimal?[]? Cartage1TaxAmountBetween { get; set; }
        public virtual decimal?[]? Cartage1TaxAmountIn { get; set; }
        public virtual decimal? CartageCharge2 { get; set; }
        public virtual decimal? CartageCharge2GreaterThanOrEqualTo { get; set; }
        public virtual decimal? CartageCharge2GreaterThan { get; set; }
        public virtual decimal? CartageCharge2LessThan { get; set; }
        public virtual decimal? CartageCharge2LessThanOrEqualTo { get; set; }
        public virtual decimal? CartageCharge2NotEqualTo { get; set; }
        public virtual decimal?[]? CartageCharge2Between { get; set; }
        public virtual decimal?[]? CartageCharge2In { get; set; }
        public virtual decimal? Cartage2TaxAmount { get; set; }
        public virtual decimal? Cartage2TaxAmountGreaterThanOrEqualTo { get; set; }
        public virtual decimal? Cartage2TaxAmountGreaterThan { get; set; }
        public virtual decimal? Cartage2TaxAmountLessThan { get; set; }
        public virtual decimal? Cartage2TaxAmountLessThanOrEqualTo { get; set; }
        public virtual decimal? Cartage2TaxAmountNotEqualTo { get; set; }
        public virtual decimal?[]? Cartage2TaxAmountBetween { get; set; }
        public virtual decimal?[]? Cartage2TaxAmountIn { get; set; }
        public virtual decimal? CartageCharge3 { get; set; }
        public virtual decimal? CartageCharge3GreaterThanOrEqualTo { get; set; }
        public virtual decimal? CartageCharge3GreaterThan { get; set; }
        public virtual decimal? CartageCharge3LessThan { get; set; }
        public virtual decimal? CartageCharge3LessThanOrEqualTo { get; set; }
        public virtual decimal? CartageCharge3NotEqualTo { get; set; }
        public virtual decimal?[]? CartageCharge3Between { get; set; }
        public virtual decimal?[]? CartageCharge3In { get; set; }
        public virtual decimal? Cartage3TaxAmount { get; set; }
        public virtual decimal? Cartage3TaxAmountGreaterThanOrEqualTo { get; set; }
        public virtual decimal? Cartage3TaxAmountGreaterThan { get; set; }
        public virtual decimal? Cartage3TaxAmountLessThan { get; set; }
        public virtual decimal? Cartage3TaxAmountLessThanOrEqualTo { get; set; }
        public virtual decimal? Cartage3TaxAmountNotEqualTo { get; set; }
        public virtual decimal?[]? Cartage3TaxAmountBetween { get; set; }
        public virtual decimal?[]? Cartage3TaxAmountIn { get; set; }
        public virtual string? CourierDetails { get; set; }
        public virtual string? CourierDetailsStartsWith { get; set; }
        public virtual string? CourierDetailsEndsWith { get; set; }
        public virtual string? CourierDetailsContains { get; set; }
        public virtual string? CourierDetailsLike { get; set; }
        public virtual string?[]? CourierDetailsBetween { get; set; }
        public virtual string?[]? CourierDetailsIn { get; set; }
        public virtual string? Notes { get; set; }
        public virtual string? NotesStartsWith { get; set; }
        public virtual string? NotesEndsWith { get; set; }
        public virtual string? NotesContains { get; set; }
        public virtual string? NotesLike { get; set; }
        public virtual string?[]? NotesBetween { get; set; }
        public virtual string?[]? NotesIn { get; set; }
        public virtual string? EmailAddress { get; set; }
        public virtual string? EmailAddressStartsWith { get; set; }
        public virtual string? EmailAddressEndsWith { get; set; }
        public virtual string? EmailAddressContains { get; set; }
        public virtual string? EmailAddressLike { get; set; }
        public virtual string?[]? EmailAddressBetween { get; set; }
        public virtual string?[]? EmailAddressIn { get; set; }
        public virtual string? StaffID { get; set; }
        public virtual string? StaffIDStartsWith { get; set; }
        public virtual string? StaffIDEndsWith { get; set; }
        public virtual string? StaffIDContains { get; set; }
        public virtual string? StaffIDLike { get; set; }
        public virtual string?[]? StaffIDBetween { get; set; }
        public virtual string?[]? StaffIDIn { get; set; }
        public virtual string? StaffTitle { get; set; }
        public virtual string? StaffTitleStartsWith { get; set; }
        public virtual string? StaffTitleEndsWith { get; set; }
        public virtual string? StaffTitleContains { get; set; }
        public virtual string? StaffTitleLike { get; set; }
        public virtual string?[]? StaffTitleBetween { get; set; }
        public virtual string?[]? StaffTitleIn { get; set; }
        public virtual string? StaffFirstName { get; set; }
        public virtual string? StaffFirstNameStartsWith { get; set; }
        public virtual string? StaffFirstNameEndsWith { get; set; }
        public virtual string? StaffFirstNameContains { get; set; }
        public virtual string? StaffFirstNameLike { get; set; }
        public virtual string?[]? StaffFirstNameBetween { get; set; }
        public virtual string?[]? StaffFirstNameIn { get; set; }
        public virtual string? StaffSurname { get; set; }
        public virtual string? StaffSurnameStartsWith { get; set; }
        public virtual string? StaffSurnameEndsWith { get; set; }
        public virtual string? StaffSurnameContains { get; set; }
        public virtual string? StaffSurnameLike { get; set; }
        public virtual string?[]? StaffSurnameBetween { get; set; }
        public virtual string?[]? StaffSurnameIn { get; set; }
        public virtual string? StaffUsername { get; set; }
        public virtual string? StaffUsernameStartsWith { get; set; }
        public virtual string? StaffUsernameEndsWith { get; set; }
        public virtual string? StaffUsernameContains { get; set; }
        public virtual string? StaffUsernameLike { get; set; }
        public virtual string?[]? StaffUsernameBetween { get; set; }
        public virtual string?[]? StaffUsernameIn { get; set; }
        public virtual byte? HistoryStatus { get; set; }
        public virtual byte? HistoryStatusGreaterThanOrEqualTo { get; set; }
        public virtual byte? HistoryStatusGreaterThan { get; set; }
        public virtual byte? HistoryStatusLessThan { get; set; }
        public virtual byte? HistoryStatusLessThanOrEqualTo { get; set; }
        public virtual byte? HistoryStatusNotEqualTo { get; set; }
        public virtual byte?[]? HistoryStatusBetween { get; set; }
        public virtual byte?[]? HistoryStatusIn { get; set; }
        public virtual short? HistoryNo { get; set; }
        public virtual short? HistoryNoGreaterThanOrEqualTo { get; set; }
        public virtual short? HistoryNoGreaterThan { get; set; }
        public virtual short? HistoryNoLessThan { get; set; }
        public virtual short? HistoryNoLessThanOrEqualTo { get; set; }
        public virtual short? HistoryNoNotEqualTo { get; set; }
        public virtual short?[]? HistoryNoBetween { get; set; }
        public virtual short?[]? HistoryNoIn { get; set; }
        public virtual string? CurrencyID { get; set; }
        public virtual string? CurrencyIDStartsWith { get; set; }
        public virtual string? CurrencyIDEndsWith { get; set; }
        public virtual string? CurrencyIDContains { get; set; }
        public virtual string? CurrencyIDLike { get; set; }
        public virtual string?[]? CurrencyIDBetween { get; set; }
        public virtual string?[]? CurrencyIDIn { get; set; }
        public virtual string? CurrencyShortName { get; set; }
        public virtual string? CurrencyShortNameStartsWith { get; set; }
        public virtual string? CurrencyShortNameEndsWith { get; set; }
        public virtual string? CurrencyShortNameContains { get; set; }
        public virtual string? CurrencyShortNameLike { get; set; }
        public virtual string?[]? CurrencyShortNameBetween { get; set; }
        public virtual string?[]? CurrencyShortNameIn { get; set; }
        public virtual string? CurrencyName { get; set; }
        public virtual string? CurrencyNameStartsWith { get; set; }
        public virtual string? CurrencyNameEndsWith { get; set; }
        public virtual string? CurrencyNameContains { get; set; }
        public virtual string? CurrencyNameLike { get; set; }
        public virtual string?[]? CurrencyNameBetween { get; set; }
        public virtual string?[]? CurrencyNameIn { get; set; }
        public virtual short? DecimalPlaces { get; set; }
        public virtual short? DecimalPlacesGreaterThanOrEqualTo { get; set; }
        public virtual short? DecimalPlacesGreaterThan { get; set; }
        public virtual short? DecimalPlacesLessThan { get; set; }
        public virtual short? DecimalPlacesLessThanOrEqualTo { get; set; }
        public virtual short? DecimalPlacesNotEqualTo { get; set; }
        public virtual short?[]? DecimalPlacesBetween { get; set; }
        public virtual short?[]? DecimalPlacesIn { get; set; }
        public virtual decimal? TotalAllocated { get; set; }
        public virtual decimal? TotalAllocatedGreaterThanOrEqualTo { get; set; }
        public virtual decimal? TotalAllocatedGreaterThan { get; set; }
        public virtual decimal? TotalAllocatedLessThan { get; set; }
        public virtual decimal? TotalAllocatedLessThanOrEqualTo { get; set; }
        public virtual decimal? TotalAllocatedNotEqualTo { get; set; }
        public virtual decimal?[]? TotalAllocatedBetween { get; set; }
        public virtual decimal?[]? TotalAllocatedIn { get; set; }
        public virtual DateTime? DueDate { get; set; }
        public virtual DateTime? DueDateGreaterThanOrEqualTo { get; set; }
        public virtual DateTime? DueDateGreaterThan { get; set; }
        public virtual DateTime? DueDateLessThan { get; set; }
        public virtual DateTime? DueDateLessThanOrEqualTo { get; set; }
        public virtual DateTime? DueDateNotEqualTo { get; set; }
        public virtual DateTime?[]? DueDateBetween { get; set; }
        public virtual DateTime?[]? DueDateIn { get; set; }
    }

    [Serializable()]
    public partial class IN_PriceSchemes
    {
        [Required]
        [PrimaryKey]
        public string? PriceSchemeID { get; set; }
        [Required]
        public string? PriceSchemeDescription { get; set; }
        [Required]
        public DateTimeOffset? LastSavedDateTime { get; set; }
        [Required]
        public bool SchemeActive { get; set; }
        [Required]
        public bool FindTheCheapest { get; set; }
        public bool IsDefault { get; set; }
    }

    [Serializable()]
    public partial class SO_Sales
    {
        public string? SalesID { get; set; }
        [Required]
        public DateTime DateRun { get; set; }
        [Required]
        public DateTime InvoiceDate { get; set; }
        [Required]
        public string? DebtorID { get; set; }
        public string? InvoiceNo { get; set; }
        public string? PartNo { get; set; }
        public string? Description { get; set; }
        public string? ClassDescription { get; set; }
        public string? Cat1Description { get; set; }
        public string? Cat2Description { get; set; }
        public string? Cat3Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? UnitSellPrice { get; set; }
        public decimal? LineTax { get; set; }
        public decimal? LineTotal { get; set; }
        public string? RunNo { get; set; }
        public short? HistoryNo { get; set; }
        public string? InvoiceID { get; set; }
        public string? InventoryID { get; set; }
        public int? YearNo { get; set; }
        public int? MonthNo { get; set; }
        [Required]
        public string? IN_LogicalID { get; set; }
        public string? Cat4Description { get; set; }
        public string? Cat5Description { get; set; }
        public short? KitStyle { get; set; }
        public short? KitStatus { get; set; }
        public string? SO_LinesID { get; set; }
        public decimal? LineCost { get; set; }
        public string? DebtorAccountNo { get; set; }
        public string? DebtorName { get; set; }
        public string? DebtorClassification { get; set; }
        public string? PhysicalWarehouse { get; set; }
        public string? LogicalWarehouse { get; set; }
        public decimal? GPDollars { get; set; }
        public decimal? GPPercent { get; set; }
    }


    [Route("/Queries/SO_Sales", "GET")]
    [ApiResponse(Description = "Read OK", StatusCode = 200)]
    [ApiResponse(Description = "Not authenticated", StatusCode = 401)]
    [ApiResponse(Description = "Not authorised", StatusCode = 403)]
    public partial class SO_SalesQuery : QueryDb<SO_Sales>
    {
        public string? SalesID { get; set; }

        public string? SalesIDStartsWith { get; set; }
        public string? SalesIDEndsWith { get; set; }
        public string? SalesIDContains { get; set; }
        public string? SalesIDLike { get; set; }
        public string?[]? SalesIDBetween { get; set; }
        public string?[]? SalesIDIn { get; set; }

        public DateTime? DateRun { get; set; }

        public DateTime? DateRunGreaterThanOrEqualTo { get; set; }
        public DateTime? DateRunGreaterThan { get; set; }
        public DateTime? DateRunLessThan { get; set; }
        public DateTime? DateRunLessThanOrEqualTo { get; set; }
        public DateTime? DateRunNotEqualTo { get; set; }
        public DateTime?[]? DateRunBetween { get; set; }
        public DateTime?[]? DateRunIn { get; set; }

        public DateTime? InvoiceDate { get; set; }

        public DateTime? InvoiceDateGreaterThanOrEqualTo { get; set; }
        public DateTime? InvoiceDateGreaterThan { get; set; }
        public DateTime? InvoiceDateLessThan { get; set; }
        public DateTime? InvoiceDateLessThanOrEqualTo { get; set; }
        public DateTime? InvoiceDateNotEqualTo { get; set; }
        public DateTime?[]? InvoiceDateBetween { get; set; }
        public DateTime?[]? InvoiceDateIn { get; set; }

        public string? DebtorID { get; set; }

        public string? DebtorIDStartsWith { get; set; }
        public string? DebtorIDEndsWith { get; set; }
        public string? DebtorIDContains { get; set; }
        public string? DebtorIDLike { get; set; }
        public string?[]? DebtorIDBetween { get; set; }
        public string?[]? DebtorIDIn { get; set; }

        public string? InvoiceNo { get; set; }

        public string? InvoiceNoStartsWith { get; set; }
        public string? InvoiceNoEndsWith { get; set; }
        public string? InvoiceNoContains { get; set; }
        public string? InvoiceNoLike { get; set; }
        public string?[]? InvoiceNoBetween { get; set; }
        public string?[]? InvoiceNoIn { get; set; } = null;
        public string? PartNo { get; set; }

        public string? PartNoStartsWith { get; set; }
        public string? PartNoEndsWith { get; set; }
        public string? PartNoContains { get; set; }
        public string? PartNoLike { get; set; }
        public string?[]? PartNoBetween { get; set; }
        public string?[]? PartNoIn { get; set; } = null;

        public string? Description { get; set; }

        public string? DescriptionStartsWith { get; set; }
        public string? DescriptionEndsWith { get; set; }
        public string? DescriptionContains { get; set; }
        public string? DescriptionLike { get; set; }
        public string?[]? DescriptionBetween { get; set; }
        public string?[]? DescriptionIn { get; set; }
        public string? ClassDescription { get; set; }

        public string? ClassDescriptionStartsWith { get; set; }
        public string? ClassDescriptionEndsWith { get; set; }
        public string? ClassDescriptionContains { get; set; }
        public string? ClassDescriptionLike { get; set; }
        public string?[]? ClassDescriptionBetween { get; set; }
        public string?[]? ClassDescriptionIn { get; set; }

        public string? Cat1Description { get; set; }

        public string? Cat1DescriptionStartsWith { get; set; }
        public string? Cat1DescriptionEndsWith { get; set; }
        public string? Cat1DescriptionContains { get; set; }
        public string? Cat1DescriptionLike { get; set; }
        public string?[]? Cat1DescriptionBetween { get; set; }
        public string?[]? Cat1DescriptionIn { get; set; } = null;
        public string? Cat2Description { get; set; }

        public string? Cat2DescriptionStartsWith { get; set; }
        public string? Cat2DescriptionEndsWith { get; set; }
        public string? Cat2DescriptionContains { get; set; }
        public string? Cat2DescriptionLike { get; set; }
        public string?[]? Cat2DescriptionBetween { get; set; }
        public string?[]? Cat2DescriptionIn { get; set; } = null;

        public string? Cat3Description { get; set; }

        public string? Cat3DescriptionStartsWith { get; set; }
        public string? Cat3DescriptionEndsWith { get; set; }
        public string? Cat3DescriptionContains { get; set; }
        public string? Cat3DescriptionLike { get; set; }
        public string?[]? Cat3DescriptionBetween { get; set; }
        public string?[]? Cat3DescriptionIn { get; set; } = null;
        public decimal? Quantity { get; set; }

        public decimal? QuantityGreaterThanOrEqualTo { get; set; }
        public decimal? QuantityGreaterThan { get; set; }
        public decimal? QuantityLessThan { get; set; }
        public decimal? QuantityLessThanOrEqualTo { get; set; }
        public decimal? QuantityNotEqualTo { get; set; }
        public decimal?[]? QuantityBetween { get; set; }
        public decimal?[]? QuantityIn { get; set; } = null;

        public decimal? UnitCost { get; set; }

        public decimal? UnitCostGreaterThanOrEqualTo { get; set; }
        public decimal? UnitCostGreaterThan { get; set; }
        public decimal? UnitCostLessThan { get; set; }
        public decimal? UnitCostLessThanOrEqualTo { get; set; }
        public decimal? UnitCostNotEqualTo { get; set; }
        public decimal?[]? UnitCostBetween { get; set; }
        public decimal?[]? UnitCostIn { get; set; } = null;

        public decimal? UnitSellPrice { get; set; }

        public decimal? UnitSellPriceGreaterThanOrEqualTo { get; set; }
        public decimal? UnitSellPriceGreaterThan { get; set; }
        public decimal? UnitSellPriceLessThan { get; set; }
        public decimal? UnitSellPriceLessThanOrEqualTo { get; set; }
        public decimal? UnitSellPriceNotEqualTo { get; set; }
        public decimal?[]? UnitSellPriceBetween { get; set; }
        public decimal?[]? UnitSellPriceIn { get; set; } = null;

        public decimal? LineTax { get; set; }

        public decimal? LineTaxGreaterThanOrEqualTo { get; set; }
        public decimal? LineTaxGreaterThan { get; set; }
        public decimal? LineTaxLessThan { get; set; }
        public decimal? LineTaxLessThanOrEqualTo { get; set; }
        public decimal? LineTaxNotEqualTo { get; set; }
        public decimal?[]? LineTaxBetween { get; set; }
        public decimal?[]? LineTaxIn { get; set; } = null;

        public decimal? LineTotal { get; set; }

        public decimal? LineTotalGreaterThanOrEqualTo { get; set; }
        public decimal? LineTotalGreaterThan { get; set; }
        public decimal? LineTotalLessThan { get; set; }
        public decimal? LineTotalLessThanOrEqualTo { get; set; }
        public decimal? LineTotalNotEqualTo { get; set; }
        public decimal?[]? LineTotalBetween { get; set; }
        public decimal?[]? LineTotalIn { get; set; } = null;

        public string? RunNo { get; set; }

        public string? RunNoStartsWith { get; set; }
        public string? RunNoEndsWith { get; set; }
        public string? RunNoContains { get; set; }
        public string? RunNoLike { get; set; }
        public string?[]? RunNoBetween { get; set; }
        public string?[]? RunNoIn { get; set; }

        public short? HistoryNo { get; set; }

        public short? HistoryNoGreaterThanOrEqualTo { get; set; }
        public short? HistoryNoGreaterThan { get; set; }
        public short? HistoryNoLessThan { get; set; }
        public short? HistoryNoLessThanOrEqualTo { get; set; }
        public short? HistoryNoNotEqualTo { get; set; }
        public short?[]? HistoryNoBetween { get; set; }
        public short?[]? HistoryNoIn { get; set; }

        public string? InvoiceID { get; set; }

        public string? InvoiceIDStartsWith { get; set; }
        public string? InvoiceIDEndsWith { get; set; }
        public string? InvoiceIDContains { get; set; }
        public string? InvoiceIDLike { get; set; }
        public string?[]? InvoiceIDBetween { get; set; }
        public string?[]? InvoiceIDIn { get; set; }

        public string? InventoryID { get; set; }

        public string? InventoryIDStartsWith { get; set; }
        public string? InventoryIDEndsWith { get; set; }
        public string? InventoryIDContains { get; set; }
        public string? InventoryIDLike { get; set; }
        public string?[]? InventoryIDBetween { get; set; }
        public string?[]? InventoryIDIn { get; set; } = null;
        public int? YearNo { get; set; }

        public int? YearNoGreaterThanOrEqualTo { get; set; }
        public int? YearNoGreaterThan { get; set; }
        public int? YearNoLessThan { get; set; }
        public int? YearNoLessThanOrEqualTo { get; set; }
        public int? YearNoNotEqualTo { get; set; }
        public int?[]? YearNoBetween { get; set; }
        public int?[]? YearNoIn { get; set; }

        public int? MonthNo { get; set; }

        public int? MonthNoGreaterThanOrEqualTo { get; set; }
        public int? MonthNoGreaterThan { get; set; }
        public int? MonthNoLessThan { get; set; }
        public int? MonthNoLessThanOrEqualTo { get; set; }
        public int? MonthNoNotEqualTo { get; set; }
        public int?[]? MonthNoBetween { get; set; }
        public int?[]? MonthNoIn { get; set; }

        public string? IN_LogicalID { get; set; }

        public string? IN_LogicalIDStartsWith { get; set; }
        public string? IN_LogicalIDEndsWith { get; set; }
        public string? IN_LogicalIDContains { get; set; }
        public string? IN_LogicalIDLike { get; set; }
        public string?[]? IN_LogicalIDBetween { get; set; }
        public string?[]? IN_LogicalIDIn { get; set; }

        public string? Cat4Description { get; set; }

        public string? Cat4DescriptionStartsWith { get; set; }
        public string? Cat4DescriptionEndsWith { get; set; }
        public string? Cat4DescriptionContains { get; set; }
        public string? Cat4DescriptionLike { get; set; }
        public string?[]? Cat4DescriptionBetween { get; set; }
        public string?[]? Cat4DescriptionIn { get; set; } = null;
        public string? Cat5Description { get; set; }

        public string? Cat5DescriptionStartsWith { get; set; }
        public string? Cat5DescriptionEndsWith { get; set; }
        public string? Cat5DescriptionContains { get; set; }
        public string? Cat5DescriptionLike { get; set; }
        public string?[]? Cat5DescriptionBetween { get; set; }
        public string?[]? Cat5DescriptionIn { get; set; } = null;

        public short? KitStyle { get; set; }

        public short? KitStyleGreaterThanOrEqualTo { get; set; }
        public short? KitStyleGreaterThan { get; set; }
        public short? KitStyleLessThan { get; set; }
        public short? KitStyleLessThanOrEqualTo { get; set; }
        public short? KitStyleNotEqualTo { get; set; }
        public short?[]? KitStyleBetween { get; set; }
        public short?[]? KitStyleIn { get; set; }

        public short? KitStatus { get; set; }

        public short? KitStatusGreaterThanOrEqualTo { get; set; }
        public short? KitStatusGreaterThan { get; set; }
        public short? KitStatusLessThan { get; set; }
        public short? KitStatusLessThanOrEqualTo { get; set; }
        public short? KitStatusNotEqualTo { get; set; }
        public short?[]? KitStatusBetween { get; set; }
        public short?[]? KitStatusIn { get; set; }

        public string? SO_LinesID { get; set; }

        public string? SO_LinesIDStartsWith { get; set; }
        public string? SO_LinesIDEndsWith { get; set; }
        public string? SO_LinesIDContains { get; set; }
        public string? SO_LinesIDLike { get; set; }
        public string?[]? SO_LinesIDBetween { get; set; }
        public string?[]? SO_LinesIDIn { get; set; } = null;

        public decimal? LineCost { get; set; }

        public decimal? LineCostGreaterThanOrEqualTo { get; set; }
        public decimal? LineCostGreaterThan { get; set; }
        public decimal? LineCostLessThan { get; set; }
        public decimal? LineCostLessThanOrEqualTo { get; set; }
        public decimal? LineCostNotEqualTo { get; set; }
        public decimal?[]? LineCostBetween { get; set; }
        public decimal?[]? LineCostIn { get; set; }

        public string? DebtorAccountNo { get; set; }

        public string? DebtorAccountNoStartsWith { get; set; }
        public string? DebtorAccountNoEndsWith { get; set; }
        public string? DebtorAccountNoContains { get; set; }
        public string? DebtorAccountNoLike { get; set; }
        public string?[]? DebtorAccountNoBetween { get; set; }
        public string?[]? DebtorAccountNoIn { get; set; }

        public string? DebtorName { get; set; }

        public string? DebtorNameStartsWith { get; set; }
        public string? DebtorNameEndsWith { get; set; }
        public string? DebtorNameContains { get; set; }
        public string? DebtorNameLike { get; set; }
        public string?[]? DebtorNameBetween { get; set; }
        public string?[]? DebtorNameIn { get; set; } = null;
        public string? DebtorClassification { get; set; }

        public string? DebtorClassificationStartsWith { get; set; }
        public string? DebtorClassificationEndsWith { get; set; }
        public string? DebtorClassificationContains { get; set; }
        public string? DebtorClassificationLike { get; set; }
        public string?[]? DebtorClassificationBetween { get; set; }
        public string?[]? DebtorClassificationIn { get; set; }

        public string? PhysicalWarehouse { get; set; }

        public string? PhysicalWarehouseStartsWith { get; set; }
        public string? PhysicalWarehouseEndsWith { get; set; }
        public string? PhysicalWarehouseContains { get; set; }
        public string? PhysicalWarehouseLike { get; set; }
        public string?[]? PhysicalWarehouseBetween { get; set; }
        public string?[]? PhysicalWarehouseIn { get; set; }
        public string? LogicalWarehouse { get; set; }

        public string? LogicalWarehouseStartsWith { get; set; }
        public string? LogicalWarehouseEndsWith { get; set; }
        public string? LogicalWarehouseContains { get; set; }
        public string? LogicalWarehouseLike { get; set; }
        public string?[]? LogicalWarehouseBetween { get; set; }
        public string?[]? LogicalWarehouseIn { get; set; }

        public decimal? GPDollars { get; set; }

        public decimal? GPDollarsGreaterThanOrEqualTo { get; set; }
        public decimal? GPDollarsGreaterThan { get; set; }
        public decimal? GPDollarsLessThan { get; set; }
        public decimal? GPDollarsLessThanOrEqualTo { get; set; }
        public decimal? GPDollarsNotEqualTo { get; set; }
        public decimal?[]? GPDollarsBetween { get; set; }
        public decimal?[]? GPDollarsIn { get; set; }

        public decimal? GPPercent { get; set; }

        public decimal? GPPercentGreaterThanOrEqualTo { get; set; }
        public decimal? GPPercentGreaterThan { get; set; }
        public decimal? GPPercentLessThan { get; set; }
        public decimal? GPPercentLessThanOrEqualTo { get; set; }
        public decimal? GPPercentNotEqualTo { get; set; }
        public decimal?[]? GPPercentBetween { get; set; }
        public decimal?[]? GPPercentIn { get; set; }

    }
    #endregion
}
#endregion 
#endregion