/* Sample WCF service with jsonP support*/

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using SiteLogixInc.PreferredFlorist.Services;


namespace SiteLogixInc.PreferredFlorist
{
    [DataContract]
    public class Customer
    {      
        [DataMember]
        public string sliFHKnownBy1;
        
        [DataMember]
        public string sliFHAddress1;

        [DataMember]
        public string sliFHAddress2;

        [DataMember]
        public string sliFHCity;

        [DataMember]
        public string sliFHState;

        [DataMember]
        public string sliFHZip;

        [DataMember]
        public string sliFHPhone;

        [DataMember]
        public string sliObitFirstName;

        [DataMember]
        public string sliObitLastName;

        [DataMember]
        public string sliObitFullName;

        [DataMember]
        public Uri sliUrl;
    }

    [DataContract]
    public class LegacyFHData
    {
        [DataMember(Name = "FuneralHome")]
        public FuneralHomeData FuneralHome { get; set; }

        [DataMember(Name = "Obituary")]
        public ObituaryData Obituary { get; set; }
    }

    [DataContract]
    public class FuneralHomeData
    {
        [DataMember(Name = "FHKnownBy1")]
        public string FHKnownBy1 { get; set; }

        [DataMember(Name = "FHAddress1")]
        public string FHAddress1 { get; set; }

        [DataMember(Name = "FHAddress2")]
        public string FHAddress2 { get; set; }

        [DataMember(Name = "FHCity")]
        public string FHCity { get; set; }

        [DataMember(Name = "FHState")]
        public string FHState { get; set; }

        [DataMember(Name = "FHZip")]
        public string FHZip { get; set; }

        [DataMember(Name = "FHPhone")]
        public string FHPhone { get; set; }
    }

    [DataContract]
    public class ObituaryData
    {
        [DataMember(Name = "FirstName")]
        public string FirstName { get; set; }

        [DataMember(Name = "LastName")]
        public string LastName { get; set; }

        [DataMember(Name = "FullName")]
        public string FullName { get; set; }

        [DataMember(Name = "Url")]
        public Uri Url { get; set; }
    }


    [ServiceContract(Namespace="JsonpAjaxService")]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class CustomerService
    {
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        public Customer GetCustomer()
        {
            var legacyJson = new LegacyFHData();
            legacyJson = GetLData();
            return new Customer()
            {
                sliFHKnownBy1 = legacyJson.FuneralHome.FHKnownBy1, 
                                    sliFHAddress1 = legacyJson.FuneralHome.FHAddress1, 
                                    sliFHAddress2 = legacyJson.FuneralHome.FHAddress2,
                                    sliFHCity = legacyJson.FuneralHome.FHCity,
                                    sliFHState = legacyJson.FuneralHome.FHState,
                                    sliFHZip = legacyJson.FuneralHome.FHZip,
                                    sliFHPhone = legacyJson.FuneralHome.FHPhone,
                                    sliObitFirstName = legacyJson.Obituary.FirstName,
                                    sliObitLastName = legacyJson.Obituary.LastName,
                                    sliObitFullName = legacyJson.Obituary.FullName,
                                    sliUrl = legacyJson.Obituary.Url
            };
        }

        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        public void EnterNewOrder()
        {
            var qsOrderid = string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["orderid"]) ? string.Empty : HttpUtility.UrlDecode(HttpContext.Current.Request.QueryString["orderid"]);
            var qsStoreid = string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["storeid"]) ? string.Empty : HttpUtility.UrlDecode(HttpContext.Current.Request.QueryString["storeid"]);
            BC_EnterNewOrder enterNewOrder = new BC_EnterNewOrder();
            enterNewOrder.Enter(qsOrderid, qsStoreid);
        }

        protected LegacyFHData GetLData()
        {
            var qsFhid = string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["fhid"]) ? string.Empty : HttpUtility.UrlDecode(HttpContext.Current.Request.QueryString["fhid"]);
            var qsPid = string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["pid"]) ? string.Empty : HttpUtility.UrlDecode(HttpContext.Current.Request.QueryString["pid"]);           
            var qsCobrand = string.IsNullOrEmpty(HttpContext.Current.Request.QueryString["cobrand"]) ? string.Empty : HttpUtility.UrlDecode(HttpContext.Current.Request.QueryString["cobrand"]);
            var legacyJsonUrl = String.Format("http://www.awebsite.com/webservices/ns/FuneralInfo.svc/GetFuneralInfoJson?fhid={0}&pid={1}&cobrand={2}", qsFhid, qsPid, qsCobrand);
            var request = (HttpWebRequest)WebRequest.Create(legacyJsonUrl);
            request.Method = "GET";
            request.ContentType = "application/json; charset=utf-8";
            var legacyData = new LegacyFHData();
            using (Stream s = request.GetResponse().GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    var jsonData = sr.ReadToEnd();
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(LegacyFHData));
                    MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonData));
                    legacyData = (LegacyFHData)serializer.ReadObject(ms);
                }
            }
            return legacyData;
        }
    }
}
