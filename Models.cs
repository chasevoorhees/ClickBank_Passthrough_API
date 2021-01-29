using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
namespace cbapiserviceconsole
{
    public static class Globals
    {
        public static string CERT_PEM_FILE = "/etc/letsencrypt/live/APIDOMAIN.com/fullchain.pem";
        public static string KEY_PEM_FILE = "/etc/letsencrypt/live/APIDOMAIN.com/privkey.pem";
        public static string LOCAL_HTTP_PORT = "5000";
        public static string LOCAL_HTTPS_PORT = "5001";
        public static string LOCAL_USER_AGENT = "CB_API_Example1.0";
        public static string CERT_MODE = "DEV";
        public static bool ENABLE_TESTSALE = false;
        public static bool SSL = true;
        public static bool CORS = true;
        public static string[] CORSList = {"https://SITEDOMAIN.com"};
        //"http://127.0.0.1",
        // public static string LOCAL_URL_LIST = "http://*:5000"; https://*:5001;
        public static string LOCAL_URL_LIST = "http://127.0.0.1:5000;http://localhost:5000;https://localhost:5001;https://127.0.0.1:5001;";
        // public static string LOCAL_URL_LIST = "https://PUBLIC_URL:5001;https://PUBLIC_IP:5001;";
        // public static string LOCAL_URL_LIST = "https://PUBLIC_URL:5001;";
        // public static string LOCAL_URL_LIST = "https://APIDOMAIN.COM:5001;";
        public class CB_API
        {
            public static string CLICKBANK_API_AUTHKEY = "DEV-KEY:API-KEY";
            public static string CURRENT_VENDOR = "vendorname";
            public static string COMMON_PRODUCT_TITLE_STRING = "Product Subscription Title";
            public static string CLICKBANK_MONTHLY_PRODUCT_SKU = "43";

            public static readonly string CLICKBANK_API_URL = "https://api.clickbank.com/rest/1.3/";
            public static readonly string CLICKBANK_API_HOST = "api.clickbank.com";
            public static readonly string ORDERS2_POST_CHANGEPRODUCT = "orders2/{0}/changeProduct";
            public static readonly string ORDERS2_POST_CHANGEPRODUCT_PARAMS = "?oldSku={0}&newSku={1}";
            public static readonly string ORDERS2_GET = "orders2/list";
            public static readonly string ORDERS2_GET_PARAMS = "?vendor={0}&email={1}&postalCode={2}&type=SALE&startDate={3}&endDate={4}";
            public static readonly string ORDERS2_GET_PARAMS_TEST = "?vendor={0}&email={1}&startDate={2}&endDate={3}&type=TEST_SALE";
            public static readonly string TICKETS_POST = "tickets/{0}";
            public static readonly string TICKETS_POST_PARAMS = "?type=cncl&reason=ticket.type.cancel.7&comment=CBAPICancel";
            public static readonly string TICKETS_GET = "tickets/list";
            public static readonly string TICKETS_GET_PARAMS = "?receipt={0}";

            //note we're not using page=# in any of our params; if you're working with many records you'll need to handle that
        }

    }
    public class APIUserInfo
    {
        public string lastName;
        public string email;
        public string zip;
        public string userIp;
    }

    [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Rule violation aceppted due blah blah..")]
    public class TicketData
    {
        public String ticketid { get; set; }
        public String receipt { get; set; }
        public String status { get; set; }
        public String type { get; set; }
        // public Comments comments { get; set; } 
        public DateTime openedDate { get; set; }
        public Object closedDate { get; set; }
        public String description { get; set; }
        //API is giving us terrible values when certain strings are null - unfortunately status is one of those strings
        //this is an easy way to clean them - otherwise we need custom get/set for dedicated models
        // private string refundType1; // field
        // public string refundType   // property
        // {
        //     get { return refundType1; }  // get method
        //     set { 
        //         try{
        //             string s = value.ToString();
        //             refundType1 = s;
        //         }catch(Exception e){
        //             refundType1 = ""; 
        //         }
        //     }  // set method
        // }
        public Object refundType { get; set; }
        // public RefundAmount refundAmount { get; set; } 
        public String customerFirstName { get; set; }
        public String customerLastName { get; set; }
        public String email { get; set; }
        public String emailAtOrderTime { get; set; }
        public Object expirationDate { get; set; }
        public String locale { get; set; }
        public Object note { get; set; }
        public String productItemNo { get; set; }
        public DateTime updateTime { get; set; }
        public String source { get; set; }
    }
    [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Rule violation aceppted due blah blah..")]
    public class TicketResponse
    {
        public List<TicketData> ticketData { get; set; }
    }
    // public class TrackingId    {
    //     public String Nil { get; set; } 
    // }

    // public class Affiliate    {
    //     public String Nil { get; set; } 
    // }

    // public class DeclinedConsent    {
    //     public String Nil { get; set; } 
    // }

    // public class Item    {
    //     public String name { get; set; } 
    //     public String value { get; set; } 
    // }

    // public class VendorVariables    {
    //     public List<Item> item { get; set; } 
    // }

    [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Rule violation aceppted due blah blah..")]
    public class LineItemData
    {
        public String itemNo { get; set; }
        public String productTitle { get; set; }
        public String recurring { get; set; }
        public String shippable { get; set; }
        public String customerAmount { get; set; }
        public String accountAmount { get; set; }
        public String quantity { get; set; }
        public String lineItemType { get; set; }
        public Object rebillAmount { get; set; }
        public Object processedPayments { get; set; }
        public Object futurePayments { get; set; }
        public Object nextPaymentDate { get; set; }
        // private string status1; // field
        // public string status   // property
        // {
        //     get { return status1; }  // get method
        //     set { 
        //         try{
        //             string s = value.ToString();
        //             status1 = s;
        //         }catch(Exception e){
        //             status1 = ""; 
        //         }
        //     }  // set method
        // }
        public Object status { get; set; }
        public String role { get; set; }
    }

    [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Rule violation aceppted due blah blah..")]
    public class OrderData
    {
        public DateTime transactionTime { get; set; }
        public String receipt { get; set; }
        // public TrackingId trackingId { get; set; } 
        public String paytmentMethod { get; set; }
        public String transactionType { get; set; }
        public String totalOrderAmount { get; set; }
        public String totalShippingAmount { get; set; }
        public String totalTaxAmount { get; set; }
        public String vendor { get; set; }
        // public Affiliate affiliate { get; set; } 
        public String country { get; set; }
        public String state { get; set; }
        public String lastName { get; set; }
        public String firstName { get; set; }
        public String currency { get; set; }
        // public DeclinedConsent declinedConsent { get; set; } 
        public String email { get; set; }
        public String postalCode { get; set; }
        public Object customerContactInfo { get; set; }
        public String role { get; set; }
        public String fullName { get; set; }
        public Object customerRefundableState { get; set; }
        // public VendorVariables vendorVariables { get; set; } 
        public LineItemData lineItemData { get; set; }
    }

    [SuppressMessage("Microsoft.Design", "IDE1006", Justification = "Rule violation aceppted due blah blah..")]
    public class OrderResponse
    {
        public List<OrderData> orderData { get; set; }
    }




}