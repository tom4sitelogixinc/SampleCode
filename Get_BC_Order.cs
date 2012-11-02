/* This code is used to download an order from a BigCommerce web store.
 * It uses the BigCommerce REST api v2 and
 * .net's Runtime.Serialization.Json
 * to download the order from BigCommerce into SQL Server.
 * The order is later processed and entered into QuickBooks using
 * the Intuit SDK.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Runtime.Serialization;
using Site_Manager.BC_Order_Objects;
using Site_Manager.DataModel;
using Site_Manager.Helpers;
using Site_Manager.Objects;
using Site_Manager.QB_Framework;

namespace Site_Manager.BC_Order_Processing
{
    class Get_BC_Order
    {
        internal Site_Manager.BC_Order_Objects.BC_Order bcOrder = new Site_Manager.BC_Order_Objects.BC_Order();
        internal long pfOrderID;

        internal OrderProcessingInfo GetOrderFromPFStore(string orderNumber, string storeName)
        {
            string storeUrl = String.Format(@"https://{0}.mybigcommerce.com/api/v2/orders/{1}", storeName, orderNumber);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(storeUrl);
            request.PreAuthenticate = true;
            request.ContentType = "application/json; charset=utf-8";
            request.Method = "GET";
            var sliInfo = new SLIinfo(storeName);
            string authInfo = String.Format(@"{0}:{1}", sliInfo.LogIn, sliInfo.APIkey);            
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo));
            request.Accept = "application/json";
            using (Stream s = request.GetResponse().GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    var jsonData = sr.ReadToEnd();
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Site_Manager.BC_Order_Objects.BC_Order));
                    MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));
                    bcOrder = (Site_Manager.BC_Order_Objects.BC_Order)serializer.ReadObject(ms);
                    request.Abort();                   
                }
            }
            
            var orderInfo = new OrderProcessingInfo();
            Site_Manager.BC_Order_Processing.Get_BC_OrderLineItems EnterOrderLineItems = new Get_BC_OrderLineItems();
            Site_Manager.BC_Order_Processing.Get_BC_OrderCoupons GetOrderCoupons = new Get_BC_OrderCoupons();
            bool orderHasCoupons = false;
            var updateOrderList = new Update_BC_Order();               
            switch (bcOrder.status_id)
            {

                /*   BigCommerce order status id's
                        0 Incomplete
                     *  1 Pending
                     *  2 Shipped
                     *  3 Partially Shipped
                     *  4 Refunded
                     *  5 Cancelled
                     *  6 Declined
                     *  7 Awaiting Payment
                     *  8 Awaiting Pickup
                     *  9 Awaiting Shipment
                     *  10 Completed
                     *  11 Awaiting Fulfillment
                     *  12 Manual Verification Required
                     */
                case 0:
                    //Incomplete
                    updateOrderList.UpdateToDoList(Convert.ToInt64(orderNumber), storeName, bcOrder.status_id);
                    orderInfo.PF_orderNumber = -1;
                    break;
                case 4:
                    //Do refund
                    updateOrderList.UpdateToDoList(Convert.ToInt64(orderNumber), storeName, bcOrder.status_id);                    
                    orderInfo.PF_orderNumber = -1;
                    break;
                case 5:
                    //Do order cancellation
                    updateOrderList.UpdateToDoList(Convert.ToInt64(orderNumber), storeName, bcOrder.status_id);                    
                    orderInfo.PF_orderNumber = -1;
                    break;
                case 6:
                    //Declined
                    updateOrderList.UpdateToDoList(Convert.ToInt64(orderNumber), storeName, bcOrder.status_id);                    
                    orderInfo.PF_orderNumber = -1;
                    break;
                case 9: 
                    //Awaiting Shipment
                    orderHasCoupons = EnterBCOrder(storeName);
                    orderInfo.PF_accountNumber = EnterBCOrderBillingAddress();
                    EnterOrderLineItems.GetOrderProductsFromPFStore(orderNumber, storeName, pfOrderID);
                    if (orderHasCoupons) { GetOrderCoupons.GetOrderCoupons(orderNumber, storeName, pfOrderID); };
                    orderInfo.PF_orderNumber = pfOrderID;
                    break;
                case 10:
                    //Completed
                    orderHasCoupons = EnterBCOrder(storeName);
                    orderInfo.PF_accountNumber = EnterBCOrderBillingAddress();
                    EnterOrderLineItems.GetOrderProductsFromPFStore(orderNumber, storeName, pfOrderID);
                    if (orderHasCoupons){ GetOrderCoupons.GetOrderCoupons(orderNumber, storeName, pfOrderID); };
                    orderInfo.PF_orderNumber = pfOrderID;
                    break;
                case 11:
                    //Awaiting Fulfillment
                    orderHasCoupons = EnterBCOrder(storeName);
                    orderInfo.PF_accountNumber =  EnterBCOrderBillingAddress();
                    EnterOrderLineItems.GetOrderProductsFromPFStore(orderNumber, storeName, pfOrderID);
                    if (orderHasCoupons){ GetOrderCoupons.GetOrderCoupons(orderNumber, storeName, pfOrderID); };
                    orderInfo.PF_orderNumber = pfOrderID;            
                    break;
                case 12:
                    //Manual Verification Required
                    updateOrderList.UpdateToDoList(Convert.ToInt64(orderNumber), storeName, bcOrder.status_id);                    
                    orderInfo.PF_orderNumber = -1;
                    break;
            }
            orderInfo.BC_OrderID = orderNumber;
            return orderInfo;
        }

        private Boolean EnterBCOrder(string storeName)
        {
            bool orderHasCoupons = false;
            long result = CheckForExistingBCOrder(storeName);
            if (result == 0)
            {
                using (PreferredFloristDBDataContext pfDB = new PreferredFloristDBDataContext())
                {
                    var bcOrderObj = new Site_Manager.DataModel.BC_Order();
                    bcOrderObj.bc_store_name = storeName;
                    bcOrderObj.base_handling_cost = bcOrder.base_handling_cost;
                    bcOrderObj.base_shipping_cost = bcOrder.base_shipping_cost;
                    bcOrderObj.base_wrapping_cost = bcOrder.base_wrapping_cost;
                    bcOrderObj.coupon_discount = bcOrder.coupon_discount;
                    if (bcOrder.coupon_discount > 0) { orderHasCoupons = true; };
                    bcOrderObj.currency_code = bcOrder.currency_code;
                    bcOrderObj.currency_exchange_rate = bcOrder.currency_exchange_rate;
                    bcOrderObj.currency_id = bcOrder.currency_id;
                    bcOrderObj.customer_id = bcOrder.customer_id; 
                    var getPFOrderNumber = new GetNextIDNumber();
                    bcOrderObj.pfOrderID = getPFOrderNumber.GetPFOrderNumber();
                    pfOrderID = (long)bcOrderObj.pfOrderID;
                    bcOrderObj.customer_message = bcOrder.customer_message;
                    var joshBD = Convert.ToDateTime("02-16-1983");
                    bcOrderObj.Date_Created = bcOrder.Date_Created < joshBD ? joshBD : bcOrder.Date_Created;
                    bcOrderObj.Date_Modified = bcOrder.Date_Modified < joshBD ? joshBD : bcOrder.Date_Modified;
                    bcOrderObj.Date_Shipped = bcOrder.Date_Shipped < joshBD ? joshBD : bcOrder.Date_Shipped;
                    bcOrderObj.requested_delivery_date = FindDeliveryDate(bcOrder.customer_message);
                    bcOrderObj.default_currency_code = bcOrder.default_currency_code;
                    bcOrderObj.default_currency_id = bcOrder.default_currency_id;
                    bcOrderObj.discount_amount = bcOrder.discount_amount;
                    bcOrderObj.geoip_country = bcOrder.geoip_country;
                    bcOrderObj.geoip_country_iso2 = bcOrder.geoip_country_iso2;
                    bcOrderObj.gift_certificate_amount = bcOrder.gift_certificate_amount;
                    bcOrderObj.handling_cost_ex_tax = bcOrder.handling_cost_ex_tax;
                    bcOrderObj.handling_cost_inc_tax = bcOrder.handling_cost_inc_tax;
                    bcOrderObj.handling_cost_tax = bcOrder.handling_cost_tax;
                    bcOrderObj.handling_cost_tax_class_id = bcOrder.handling_cost_tax_class_id;
                    bcOrderObj.id = bcOrder.id;
                    bcOrderObj.ip_address = bcOrder.ip_address;
                    bcOrderObj.is_deleted = bcOrder.is_deleted;
                    bcOrderObj.items_shipped = bcOrder.items_shipped;
                    bcOrderObj.items_total = bcOrder.items_total;
                    bcOrderObj.order_is_digital = bcOrder.order_is_digital;
                    bcOrderObj.payment_method = bcOrder.payment_method;
                    bcOrderObj.payment_provider_id = bcOrder.payment_provider_id;
                    bcOrderObj.payment_status = bcOrder.payment_status;
                    bcOrderObj.refunded_amount = bcOrder.refunded_amount;
                    bcOrderObj.shipping_address_count = bcOrder.shipping_address_count;
                    bcOrderObj.shipping_cost_ex_tax = bcOrder.shipping_cost_ex_tax;
                    bcOrderObj.shipping_cost_inc_tax = bcOrder.shipping_cost_inc_tax;
                    bcOrderObj.shipping_cost_tax = bcOrder.shipping_cost_tax;
                    bcOrderObj.shipping_cost_tax_class_id = bcOrder.shipping_cost_tax_class_id;
                    bcOrderObj.staff_notes = bcOrder.staff_notes;
                    /*  0 Incomplete
                     *  1 Pending
                     *  2 Shipped
                     *  3 Partially Shipped
                     *  4 Refunded
                     *  5 Cancelled
                     *  6 Declined
                     *  7 Awaiting Payment
                     *  8 Awaiting Pickup
                     *  9 Awaiting Shipment
                     *  10 Completed
                     *  11 Awaiting Fulfillment
                     *  12 Manual Verification Required
                     */
                    bcOrderObj.status = bcOrder.status;
                    bcOrderObj.status_id = bcOrder.status_id;
                    bcOrderObj.store_credit_amount = bcOrder.store_credit_amount;
                    bcOrderObj.subtotal_ex_tax = bcOrder.subtotal_ex_tax;
                    bcOrderObj.subtotal_inc_tax = bcOrder.subtotal_inc_tax;
                    bcOrderObj.subtotal_tax = bcOrder.subtotal_tax;
                    bcOrderObj.total_ex_tax = bcOrder.total_inc_tax - bcOrder.total_tax;
                    bcOrderObj.total_inc_tax = bcOrder.total_inc_tax;
                    bcOrderObj.total_tax = bcOrder.total_tax;
                    bcOrderObj.wrapping_cost_ex_tax = bcOrder.wrapping_cost_ex_tax;
                    bcOrderObj.wrapping_cost_inc_tax = bcOrder.wrapping_cost_inc_tax;
                    bcOrderObj.wrapping_cost_tax = bcOrder.wrapping_cost_tax;
                    bcOrderObj.wrapping_cost_tax_class_id = bcOrder.wrapping_cost_tax_class_id;
                    decimal orderTotal = bcOrder.total_inc_tax;
                    bcOrderObj.merchant_fee = orderTotal * .03M;
                    pfDB.BC_Orders.InsertOnSubmit(bcOrderObj);
                    pfDB.SubmitChanges();
                    var orderListObj = (from e1 in pfDB.BC_OrderLists where e1.bc_order_id == bcOrder.id && e1.bc_store_name == storeName select e1).SingleOrDefault();
                    orderListObj.requested_delivery_date = bcOrderObj.requested_delivery_date;
                    pfDB.SubmitChanges();
                }
            }
            return orderHasCoupons;        
        }

        private long CheckForExistingBCOrder(string storeName)
        {
            using (var pfDB = new PreferredFloristDBDataContext())
            {
                var orderObj = from pi in pfDB.BC_Orders
                               where pi.id == bcOrder.id &&
                                     pi.bc_store_name == storeName
                              select pi;
                
                if (orderObj == null)
                {
                    return 0;
                }
                else
                {
                    return Convert.ToInt64(orderObj.Count(p => p.bc_store_name == storeName));
                }
            }
        }

        private long EnterBCOrderBillingAddress()   //Equates to a QuickBooks customer
        {
            using (PreferredFloristDBDataContext pfDB = new PreferredFloristDBDataContext())
            {
                var bcOrderAddressObj = new Site_Manager.DataModel.BC_BillingAddress();
                bcOrderAddressObj.bc_order_id = bcOrder.id;
                bcOrderAddressObj.city = bcOrder.billing_address.city;
                bcOrderAddressObj.company = bcOrder.billing_address.company;
                bcOrderAddressObj.country = bcOrder.billing_address.country;
                bcOrderAddressObj.country_iso2 = bcOrder.billing_address.country_iso2;
                bcOrderAddressObj.email = bcOrder.billing_address.email;
                bcOrderAddressObj.first_name = bcOrder.billing_address.first_name;
                bcOrderAddressObj.last_name = bcOrder.billing_address.last_name;
                var validateField = new ValidateFieldLength();
                bcOrderAddressObj.phone = validateField.PhoneNumber(bcOrder.billing_address.phone);
                bcOrderAddressObj.state = bcOrder.billing_address.state;
                bcOrderAddressObj.street_1 = bcOrder.billing_address.street_1;
                bcOrderAddressObj.street_2 = bcOrder.billing_address.street_2;
                bcOrderAddressObj.zip = bcOrder.billing_address.zip;
                if (null != bcOrder.total_tax && bcOrder.total_tax > 0)
                {
                    bcOrderAddressObj.IsTaxable = true;
                }
                else
                {
                    bcOrderAddressObj.IsTaxable = false;
                }
                var GetAccountNumber = new GetNextIDNumber();
                bcOrderAddressObj.PF_accountNumber = GetAccountNumber.GetCustomerNumber();
                pfDB.BC_BillingAddresses.InsertOnSubmit(bcOrderAddressObj);
                pfDB.SubmitChanges();
                return (long)bcOrderAddressObj.PF_accountNumber;
            }
        }

        static DateTime FindDeliveryDate(string text)
        {
            DateTime dt2 = System.DateTime.Now.AddDays(3); //Failsafe in case something blows up
            try
            {
                const string Prefix = "Delivery Date:";
                int start = text.IndexOf(Prefix);
                if (start == -1)
                {
                    return dt2; // Or throw an exception
                }
                int end = start + Prefix.Length;
                if (end == -1)
                {
                    return dt2; // Or throw an exception
                }
                const string suffix = "Deliver To:";
                int startSuffix = text.IndexOf(suffix);
                if (startSuffix == -1)
                {
                    return dt2; // Or throw an exception
                }
                int length = startSuffix - end;

                string textTrimmed = text.Substring(end, length).Trim();
                DateTime dt = Convert.ToDateTime(textTrimmed);
                return dt;
            }
            catch
            {
                return dt2;
            }
        }
    }
}
