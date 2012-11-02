/* This code will create an invoice in QuickBooks 2012.
 * Portions of this code were borrowed with permission
 * from the Intuit SDK.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using Site_Manager.DataModel;
using Interop.QBFC11;
using System.Collections;
using Site_Manager.Objects;
using Site_Manager.Helpers;
using Site_Manager.Session_Framework;
using System.Text;
using System.Text.RegularExpressions;

namespace Site_Manager.QBMethods
{
    internal partial class AddInvoiceToQuickBooks
    {

        protected int qbTranasctionResponse = -1;
        protected string qbTransactionResponseDetail = string.Empty;
        public string qbTrxnId = string.Empty;
        protected string qbListIDforThisTransaction = string.Empty;
        public string qbEditSequence = string.Empty;
        protected Site_Manager.DataModel.BC_BillingAddress customer = new Site_Manager.DataModel.BC_BillingAddress();

        public QBRequest DoInvoiceAdd(QBRequest wqObject)
        {
            
            bool sessionBegun = false;
            bool connectionOpen = false;

            //Create the session Manager object
            var sessionManager = new Interop.QBFC11.QBSessionManager();          

                    try
                    {
                        IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", wqObject.QBVersion, 0);  //originally was 7 qbxml version supported # 1.1 March 2002 # 2.0 November 2002 # 2.1 June 2003 # 3.0 November 2003 # 4.0 November 2004 # 4.1 June 2005 # 5.0 November 2005 # 6.0 October 2006 
                        requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;
                        try
                        {
                            using (var db = new PreferredFloristDBDataContext())
                            {
                                customer = db.BC_BillingAddresses.FirstOrDefault(p => p.PF_accountNumber == wqObject.CustomerID);
                            }
                        }
                        catch (Exception ex)
                        {
                            CatchHandler ch = new CatchHandler();
                            ch.CaptureError(ex);
                        }
                        BuildInvoiceAddRq(requestMsgSet, customer, wqObject);


                        //Connect to QuickBooks and begin a session
                        sessionManager.OpenConnection("", "Site Manager");
                        connectionOpen = true;
                        try { sessionManager.BeginSession("", ENOpenMode.omMultiUser); }
                        catch { sessionManager.BeginSession("", ENOpenMode.omSingleUser); }
                        sessionBegun = true;

                        //Send the request and get the response from QuickBooks
                        IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);


                        //End the session and close the connection to QuickBooks
                        sessionManager.EndSession();
                        sessionBegun = false;
                        sessionManager.CloseConnection();
                        connectionOpen = false;

                        WalkInvoiceAddRs(responseMsgSet);                        

                    }
                    catch (Exception ex)
                    {
                        if (sessionBegun)
                        {
                            sessionManager.EndSession();

                        }
                        if (connectionOpen)
                        {
                            sessionManager.CloseConnection();
                        }
                        qbTranasctionResponse = 99;
                        qbTransactionResponseDetail = String.Format("AddInvoiceInQuickBooks failed. {0} {1}", ex.InnerException.Message, qbTransactionResponseDetail);
                        CatchHandler ch = new CatchHandler();
                        ch.CaptureError(ex);

                    }
                    wqObject.QBListID = qbListIDforThisTransaction;
                    wqObject.Status = qbTranasctionResponse;
                    wqObject.StatusMessage = qbTransactionResponseDetail;
                    return wqObject;
        }

        void BuildInvoiceAddRq(IMsgSetRequest requestMsgSet, Site_Manager.DataModel.BC_BillingAddress customer, QBRequest wqObject)
        {
            IInvoiceAdd InvoiceAddRq = requestMsgSet.AppendInvoiceAddRq();
            var invoice = new Site_Manager.DataModel.BC_Order();
            using (PreferredFloristDBDataContext pfDB = new PreferredFloristDBDataContext())
            {
                invoice = pfDB.BC_Orders.FirstOrDefault(p => p.pfOrderID == wqObject.InvoiceNumber);                
            }
            InvoiceAddRq.defMacro.SetValue("1234");
            //Set field value for ListID
            InvoiceAddRq.CustomerRef.ListID.SetValue(wqObject.QBListID);
            //Set field value for FullName
            InvoiceAddRq.ARAccountRef.FullName.SetValue("Accounts Receivable");
            InvoiceAddRq.ClassRef.FullName.SetValue(String.Format("{0}:Orders",wqObject.QBClass));
            //Set field value for FullName
            InvoiceAddRq.TemplateRef.FullName.SetValue("PF Invoice Template");
            //Set field value for TxnDate   DateTime.Parse("12/15/2007")
            if ((invoice.Date_Created == null))
                InvoiceAddRq.TxnDate.SetValue(Convert.ToDateTime(System.DateTime.Now.ToShortDateString()));
            else
                InvoiceAddRq.TxnDate.SetValue(Convert.ToDateTime(invoice.Date_Created.ToString()));
           //Set field value for RefNumber
            InvoiceAddRq.RefNumber.SetValue(wqObject.InvoiceNumber.ToString());
            InvoiceAddRq.RefNumber.SetValue(String.Format("{0}-{1}", wqObject.SiteID, wqObject.BC_OrderID));
            //Set field value for Addr1
            InvoiceAddRq.BillAddress.Addr1.SetValue(String.Format("{0} {1}", customer.first_name, customer.last_name));
            //Set field value for Addr2
            InvoiceAddRq.BillAddress.Addr2.SetValue(customer.street_1);
            //Set field value for City
            InvoiceAddRq.BillAddress.City.SetValue(customer.city);
            //Set field value for State
            InvoiceAddRq.BillAddress.State.SetValue(customer.state);
            //Set field value for PostalCode
            InvoiceAddRq.BillAddress.PostalCode.SetValue(customer.zip);
            //Set field value for Country
            InvoiceAddRq.BillAddress.Country.SetValue(customer.country);
            InvoiceAddRq.IsPending.SetValue(false);
            //Set field value for PONumber
            InvoiceAddRq.PONumber.SetValue(String.Empty);
            //Set field value for FullName
            InvoiceAddRq.TermsRef.FullName.SetValue("Due on receipt");
             //Set field value for ShipDate DateTime.Parse("12/15/2007")
            InvoiceAddRq.ShipDate.SetValue((invoice.Date_Created == null) ? Convert.ToDateTime(System.DateTime.Now.ToShortDateString()) : Convert.ToDateTime(invoice.Date_Created.ToString()));           
            ////Set field value for ListID
            //InvoiceAddRq.ShipMethodRef.ListID.SetValue("200000-1011023419");
            //Set field value for FullName
            //InvoiceAddRq.ShipMethodRef.FullName.SetValue("ab");
            //Set field value for FullName
            InvoiceAddRq.ItemSalesTaxRef.FullName.SetValue("In State");
            //Set field value for FullName
            InvoiceAddRq.CustomerMsgRef.FullName.SetValue("Thank you for shopping at preferredflorist.com");
            //Set field value for IsToBePrinted
            InvoiceAddRq.IsToBePrinted.SetValue(false);
            //Set field value for IsToBeEmailed           
            //InvoiceAddRq.IsToBeEmailed.SetValue(isValid);
            InvoiceAddRq.IsToBeEmailed.SetValue(false);
            //InvoiceAddRq.FOB.SetValue(invoice.CustomerJobNumber);
            //InvoiceAddRq.Other.SetValue(invoice.WorkOrderNumber.ToString());


            using (var db = new PreferredFloristDBDataContext())
            {
                int countOfInvolis = 0;
                var bagOfInvolis = from pi in db.BC_OrderLineItems
                                   where pi.PFOrderID == wqObject.InvoiceNumber
                                   select pi;
                countOfInvolis = bagOfInvolis.Count();
                if (countOfInvolis > 0)
                {
                    foreach (Site_Manager.DataModel.BC_OrderLineItem involi in bagOfInvolis) 
                    {
                        IORInvoiceLineAdd ORInvoiceLineAddListElement1 = InvoiceAddRq.ORInvoiceLineAddList.Append();
                        string ORInvoiceLineAddListElementType2 = "InvoiceLineAdd";
                        if (ORInvoiceLineAddListElementType2 == "InvoiceLineAdd")
                        {
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.ItemRef.FullName.SetValue("Product");

                            //Get the value of the card sentiment, card signature, verse, ribbon etc.
                            var bagOfProductOptions = from pi in db.BC_OrderProductOptions
                                                      where pi.PF_Line_Item_ID == involi.pf_line_item_id
                                                      orderby pi.option_id ascending
                                                      select pi;
                            var prodOptsText = new StringBuilder();
                            foreach (Site_Manager.DataModel.BC_OrderProductOption oPo in bagOfProductOptions)
                            {                                
                                prodOptsText.Append(String.Format("{0}:\r\n", oPo.display_name));
                                prodOptsText.Append(String.Format("{0}:\r\n\r\n", oPo.value));
                            }
                            //Set field value for Desc
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.Desc.SetValue(String.Format("{0}   {1}\r\n\r\n{2}\r\n\r\n", involi.sku, involi.name, prodOptsText));
                            //Set field value for Quantity
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.Quantity.SetValue(Convert.ToInt32(involi.quantity));

                            ORInvoiceLineAddListElement1.InvoiceLineAdd.ORRatePriceLevel.Rate.SetValue(Convert.ToDouble(involi.price_ex_tax));
                            if (null == customer.IsTaxable || customer.IsTaxable == false)
                            {
                                ORInvoiceLineAddListElement1.InvoiceLineAdd.SalesTaxCodeRef.FullName.SetValue("Non");
                            }
                            else
                            {
                                ORInvoiceLineAddListElement1.InvoiceLineAdd.SalesTaxCodeRef.FullName.SetValue("Tax");
                            }
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.Other1.SetValue(involi.sku);               


                        }
                    }

                    //Add order coupons if there are any
                    if (invoice.coupon_discount > 0)
                    {
                        int countOfCoupons = 0;
                        var bagOfCoupons = from pi in db.BC_OrderCoupons
                                           where pi.pf_Order_ID == wqObject.InvoiceNumber
                                           select pi;
                        countOfCoupons = bagOfCoupons.Count();
                        if (countOfCoupons > 0)
                        {
                            foreach (Site_Manager.DataModel.BC_OrderCoupon couponLi in bagOfCoupons)
                            {
                                IORInvoiceLineAdd ORInvoiceLineAddListElement1 = InvoiceAddRq.ORInvoiceLineAddList.Append();
                                string ORInvoiceLineAddListElementType2 = "InvoiceLineAdd";
                                if (ORInvoiceLineAddListElementType2 == "InvoiceLineAdd")
                                {
                                    ORInvoiceLineAddListElement1.InvoiceLineAdd.ItemRef.FullName.SetValue("Coupon");
                                    //Set field value for Desc
                                    ORInvoiceLineAddListElement1.InvoiceLineAdd.Desc.SetValue(couponLi.code);
                                    //Set field value for Quantity
                                    //ORInvoiceLineAddListElement1.InvoiceLineAdd.Quantity.SetValue(1); //Cannot set quantity for item of this type

                                    ORInvoiceLineAddListElement1.InvoiceLineAdd.ORRatePriceLevel.Rate.SetValue(Convert.ToDouble(invoice.coupon_discount));
                                    if (null == customer.IsTaxable || customer.IsTaxable == false)
                                    {
                                        ORInvoiceLineAddListElement1.InvoiceLineAdd.SalesTaxCodeRef.FullName.SetValue("Non");
                                    }
                                    else
                                    {
                                        ORInvoiceLineAddListElement1.InvoiceLineAdd.SalesTaxCodeRef.FullName.SetValue("Tax");
                                    }
                                    ORInvoiceLineAddListElement1.InvoiceLineAdd.Other1.SetValue(couponLi.code);
                                }
                            }
                        }
                    }                   
               
                        //Add Service Fee. Note: in some of the earlier stores, the handling fee was billed as shipping
                    
                        if (invoice.handling_cost_ex_tax > 0)
                        {
                            IORInvoiceLineAdd ORInvoiceLineAddListElement1 = InvoiceAddRq.ORInvoiceLineAddList.Append();
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.ItemRef.FullName.SetValue("Service Fee");
                            //Set field value for Desc
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.Desc.SetValue("Service Fee");
                            //Set field value for Quantity
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.Quantity.SetValue(1);
                            //ORInvoiceLineAddListElement1.InvoiceLineAdd.Other1.SetValue(dr["PartNumber"].ToString());

                            ORInvoiceLineAddListElement1.InvoiceLineAdd.Amount.SetValue(Convert.ToDouble(invoice.handling_cost_ex_tax));
                            if (invoice.handling_cost_tax > 0)
                            {
                                ORInvoiceLineAddListElement1.InvoiceLineAdd.SalesTaxCodeRef.FullName.SetValue("Tax");
                            }
                            else
                            {
                                ORInvoiceLineAddListElement1.InvoiceLineAdd.SalesTaxCodeRef.FullName.SetValue("Non");                            }
                        }

                        if (invoice.shipping_cost_ex_tax > 0) // Note: in some of the earlier stores, the handling fee was billed as shipping
                        {
                            IORInvoiceLineAdd ORInvoiceLineAddListElement1 = InvoiceAddRq.ORInvoiceLineAddList.Append();
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.ItemRef.FullName.SetValue("Shipping");
                            //Set field value for Desc
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.Desc.SetValue("Shipping");
                            //Set field value for Quantity
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.Quantity.SetValue(1);
                            
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.Amount.SetValue(Convert.ToDouble(invoice.shipping_cost_ex_tax));
                            if (invoice.shipping_cost_tax > 0)
                            {
                                ORInvoiceLineAddListElement1.InvoiceLineAdd.SalesTaxCodeRef.FullName.SetValue("Tax");
                            }
                            else
                            {
                                ORInvoiceLineAddListElement1.InvoiceLineAdd.SalesTaxCodeRef.FullName.SetValue("Non"); //////////////////HEEEEEEEEEEEEEEY??????????????????
                            }
                        }
                    

                        //Add Order Discounts
                        if (!(String.IsNullOrEmpty(invoice.discount_amount.ToString())) && invoice.discount_amount > 0)
                        {
                            Decimal aCredit = invoice.discount_amount > 0 ? (decimal)invoice.discount_amount : 0;
                            IORInvoiceLineAdd ORInvoiceLineAddListElement1 = InvoiceAddRq.ORInvoiceLineAddList.Append();
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.ItemRef.FullName.SetValue("Discount");
                            //Set field value for Desc
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.Desc.SetValue("Discount");
                            
                            ORInvoiceLineAddListElement1.InvoiceLineAdd.Amount.SetValue(Convert.ToDouble(aCredit));
                            if (null == customer.IsTaxable || customer.IsTaxable == false)
                            {
                                ORInvoiceLineAddListElement1.InvoiceLineAdd.SalesTaxCodeRef.FullName.SetValue("Non");
                            }
                            else
                            {
                                ORInvoiceLineAddListElement1.InvoiceLineAdd.SalesTaxCodeRef.FullName.SetValue("Tax");
                            }
                        }                   
                }
            }
        }

        void WalkInvoiceAddRs(IMsgSetResponse responseMsgSet)
        {
            if (responseMsgSet == null) return;

            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null) return;

            //if we sent only one request, there is only one response, we'll walk the list for this sample
            for (int i = 0; i < responseList.Count; i++)
            {
                IResponse response = responseList.GetAt(i);
                //check the status code of the response, 0=ok, >0 is warning
                if (response.StatusCode >= 0)//0 means it was successfully transacted
                {
                    qbTranasctionResponse = response.StatusCode;
                    qbTransactionResponseDetail = response.StatusMessage;

                    //the request-specific response is in the details, make sure we have some
                    if (response.Detail != null)
                    {
                        //make sure the response is the type we're expecting
                        ENResponseType responseType = (ENResponseType)response.Type.GetValue();
                        if (responseType == ENResponseType.rtInvoiceAddRs)         ////you will get to here even if the status is 0 (ok)
                        {
                            //upcast to more specific type here, this is safe because we checked with response.Type check above
                            IInvoiceRet InvoiceRet = (IInvoiceRet)response.Detail;
                            WalkInvoiceRet(InvoiceRet);
                        }
                    }
                }
            }
        }
        
        void WalkInvoiceRet(IInvoiceRet InvoiceRet)
        {
            if (InvoiceRet == null) return;
            //Go through all the elements of IInvoiceRet
            //Get value of TxnID
            qbTrxnId = (string)InvoiceRet.TxnID.GetValue();
            //Get value of TimeCreated
            DateTime TimeCreated7 = (DateTime)InvoiceRet.TimeCreated.GetValue();
            //Get value of TimeModified
            DateTime TimeModified8 = (DateTime)InvoiceRet.TimeModified.GetValue();
            //Get value of EditSequence
            qbEditSequence = (string)InvoiceRet.EditSequence.GetValue();
           
        }

        public static bool isEmail(string inputEmail)
        {
            const string expression = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
            @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
            @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";

            Regex regex = new Regex(expression);
            return regex.IsMatch(inputEmail);
        }
    }
}


   

     