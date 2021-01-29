using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Http.Json;
using System.Diagnostics.CodeAnalysis;

namespace cbapiserviceconsole 
{
    public class ClickBaseService
    {
        private static List<APIClient> clientList = new();
        public record APIClient(string IP, string Email, string CallStatus, DateTime CallTimeStamp);


        public static HttpClient CreateFreshHttpClient()
        {
            HttpClient client = new();
            client.BaseAddress = new("https://api.clickbank.com");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", Globals.CB_API.CLICKBANK_API_AUTHKEY);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Access-Control-Allow-Origin", "*");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Access-Control-Allow-Headers", "accept, authorization, Content-Type");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Access-Control-Allow-Methods", "PUT, POST, GET, DELETE, PATCH, OPTIONS");

            client.DefaultRequestHeaders.Host = Globals.CB_API.CLICKBANK_API_HOST;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("User-Agent", Globals.LOCAL_USER_AGENT);

            return client;
        }

        public Task<string> RequestCancel(APIUserInfo userInfo)
        {//Return: 409 "FAIL" 200 "OK" 201 "ALREADY CANCL" 404 "NOT FOUND" 403 "REJECT"
            List<APIClient> attempts = clientList.Where(c => c.IP == userInfo.userIp).ToList();
            var count = 0;
            if (attempts != null)
            {
                count = attempts.Count;
            }
            string receipt = "NF";
            List<String> receiptList = new();
            DateTime deactivateDate = DateTime.UtcNow.AddDays(1);
            string result = "ACCEPT";
            int acceptCount = 0;
            if (count > 0)
            {
                foreach (APIClient c in attempts)
                {
                    if (c.CallStatus == "ACCEPT")
                    {
                        acceptCount++;
                    }
                    if (DateTime.UtcNow > c.CallTimeStamp)
                    {
                        clientList.Remove(c);
                        attempts.Remove(c);
                    }
                }
            }

            attempts = clientList.Where(c => c.IP == userInfo.userIp).ToList();
            count = 0;
            if (attempts != null)
            {
                count = attempts.Count;
            }
            if (count > 6)
            {
                Console.WriteLine("API Call Rejected    Email: " + userInfo.email + "    IP: " + userInfo.userIp + "    IP Attempts/24hr: " + count);
                result = "REJECT";
            }

            HttpRequestMessage request = null;
            OrderResponse orderResponse = null;
            HttpClient client = null;
            HttpResponseMessage response = null;

            GetOrders2ActiveRecurring(ref userInfo, ref orderResponse, ref request, ref response, ref client, ref result, ref receiptList);

            if (result == "ACCEPT" && receiptList.Count > 0)
            {
                GetTickets(ref request, ref response, ref client, ref result, ref receiptList);
            }
            else
            {
                Console.WriteLine("Error - Result wasn't accept? RESULT: " + result + "   or missing receipt string?: RECEIPT: " + receipt);
            }


            if (result == "ACCEPT" && receiptList.Count > 0)
            {
                PostTicketCancel(ref request, ref response, ref client, ref result, ref receiptList, ref receipt);
            }


            if (receiptList.Count > 0 && result == "OK")
            {
                result = "OK MORE";
                Console.WriteLine("NOTE: Next API Call - Ticket Completed for Email: " + userInfo.email + "    Receipt: " + receipt.ToString());
                Console.WriteLine("NOTE: Next API Call - Found More Subscriptions for User: #" + receiptList.Count.ToString());
            }
            else
            {
                if (!String.IsNullOrEmpty(userInfo.userIp) && userInfo.userIp != "127.0.0.1" && userInfo.userIp != "localhost" && userInfo.lastName != "testsale")
                {
                    APIClient newClient = new(userInfo.userIp, userInfo.email, result, deactivateDate);
                    clientList.Add(newClient);
                }
                else
                {
                    String ipStr = "NOT FOUND";
                    if (!String.IsNullOrEmpty(userInfo.userIp))
                    {
                        ipStr = userInfo.userIp;
                    }
                    if (userInfo.lastName == "testsale")
                    {
                        Console.WriteLine("NOTE: Uncounted TESTING API Call for: " + userInfo.email + "    from: " + ipStr);
                    }
                    else
                    {
                        Console.WriteLine("NOTE: Uncounted API Call for: " + userInfo.email + "    from: " + ipStr);
                    }
                }
            }

            client.Dispose();

            Console.WriteLine("API Call Completed: Cancel");
            Console.WriteLine("Email: " + userInfo.email + "    IP: " + userInfo.userIp + "    Receipt#: " + receipt.ToString() + "    Final Status: " + result);
            Console.WriteLine("API Calls Incomplete (Stuck on ACCEPT)/24hr: " + acceptCount);
            Console.WriteLine("Total API Calls/24hr (Not counting local or OK MORE): " + clientList.Count);

            return Task.FromResult(result);
        }
        public Task<string> RequestChange19(APIUserInfo userInfo)
        {//Return: 409 "FAIL" 200 "OK" 201 "ALREADY 19" 404 "NOT FOUND" 403 "REJECT"
            List<APIClient> attempts = clientList.Where(c => c.IP == userInfo.userIp).ToList();
            var count = 0;
            if (attempts != null)
            {
                count = attempts.Count;
            }
            string receipt = "NF";
            List<String> receiptList = new();
            DateTime deactivateDate = DateTime.UtcNow.AddDays(1);
            string result = "ACCEPT";
            int acceptCount = 0;
            if (count > 0)
            {
                foreach (APIClient c in attempts)
                {
                    if (c.CallStatus == "ACCEPT")
                    {
                        acceptCount++;
                    }
                    if (DateTime.UtcNow > c.CallTimeStamp)
                    {
                        clientList.Remove(c);
                        attempts.Remove(c);
                    }
                }
            }

            attempts = clientList.Where(c => c.IP == userInfo.userIp).ToList();
            count = 0;
            if (attempts != null)
            {
                count = attempts.Count;
            }
            if (count > 6)
            {
                Console.WriteLine("API Call Rejected    Email: " + userInfo.email + "    IP: " + userInfo.userIp + "    IP Attempts/24hr: " + count);
                result = "REJECT";
            }


            HttpRequestMessage request = null;
            OrderResponse orderResponse = null;
            HttpClient client = null;
            HttpResponseMessage response = null;
            LineItemData orderLineItemData = null;

            bool foundNewSKU = GetOrders2ActiveRecurring_NotSKU(Globals.CB_API.CLICKBANK_MONTHLY_PRODUCT_SKU, ref orderLineItemData, ref userInfo, ref orderResponse, ref request, ref response, ref client, ref result, ref receiptList, ref receipt);

            if (foundNewSKU)
            {
                //found order that's not cancelled - work to do
                //let's skip the ticket check and just POST changeOrder 
                //NOTE: THIS MAY CAUSE A PROBLEM IF THERE'S A PENDING CANCEL OR REFUND TICKET FOR THE GIVEN ORDER/RECEIPT
                //      Unlikely but would be safer to check for existing ticket first and cancel it if pending or return an error

                PostOrders2ChangeOrder(Globals.CB_API.CLICKBANK_MONTHLY_PRODUCT_SKU, ref orderLineItemData, ref request, ref response, ref client, ref result, ref receiptList, ref receipt);

            }

            if (receiptList.Count > 0 && result == "OK")
            {
                result = "OK MORE";
                Console.WriteLine("NOTE: Next API Call - ChangeOrder Completed for Email: " + userInfo.email + "    Receipt: " + receipt.ToString());
                Console.WriteLine("NOTE: Next API Call - Found More Than 1 Subscription for User: #" + receiptList.Count.ToString());
            }
            else
            {
                if (!String.IsNullOrEmpty(userInfo.userIp) && userInfo.userIp != "127.0.0.1" && userInfo.userIp != "localhost" && userInfo.lastName != "testsale")
                {
                    APIClient newClient = new(userInfo.userIp, userInfo.email, result, deactivateDate);
                    clientList.Add(newClient);
                }
                else
                {
                    String ipStr = "NOT FOUND";
                    if (!String.IsNullOrEmpty(userInfo.userIp))
                    {
                        ipStr = userInfo.userIp;
                    }
                    if (userInfo.lastName == "testsale")
                    {
                        Console.WriteLine("NOTE: Uncounted TESTING API Call for: " + userInfo.email + "    from: " + ipStr);
                    }
                    else
                    {
                        Console.WriteLine("NOTE: Uncounted API Call for: " + userInfo.email + "    from: " + ipStr);
                    }
                }
            }

            client.Dispose();

            Console.WriteLine("API Call Completed: ChangeOrder 19");
            Console.WriteLine("Email: " + userInfo.email + "    IP: " + userInfo.userIp + "    Receipt#: " + receipt.ToString() + "    Final Status: " + result);
            Console.WriteLine("API Calls Incomplete (Stuck on ACCEPT)/24hr: " + acceptCount);
            Console.WriteLine("Total API Calls/24hr (Not counting local or OK MORE): " + clientList.Count);

            return Task.FromResult(result);
        }
        public static bool PostTicketCancel(ref HttpRequestMessage request, ref HttpResponseMessage response, ref HttpClient client, ref string result, ref List<String> receiptList, ref string receipt)
        {
            receipt = receiptList[0];
            receiptList.Remove(receipt);

            string ticketsPostEndPoint = String.Format(Globals.CB_API.TICKETS_POST, receipt);
            string ticketsPostParams = Globals.CB_API.TICKETS_POST_PARAMS;
            string ticketsPostURL = Globals.CB_API.CLICKBANK_API_URL + ticketsPostEndPoint + ticketsPostParams;

            client.Dispose();
            client = CreateFreshHttpClient();

            request = new()
            {
                RequestUri = new(ticketsPostURL),
                Method = HttpMethod.Post,
            };

            response = client.SendAsync(request).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
                                                          // IEnumerable<Ticket> ticketGetResponse = response.Content.ReadFromJsonAsync<IEnumerable<Ticket>>().Result;

            if (response != null && response.IsSuccessStatusCode) //do we care about ticketpostresponse here?
            {
                result = "OK";
            }
            else
            {
                if (response == null)
                {
                    Console.WriteLine("ticket post API null response");
                    result = "FAIL";
                }
                else
                {
                    if (response.IsSuccessStatusCode)
                    {
                        //probably ok here
                    }
                    else
                    {
                        Console.WriteLine("ticket post API fail");
                        result = "FAIL";
                    }
                }
            }

            return true;

        }

        public static bool GetTickets(ref HttpRequestMessage request, ref HttpResponseMessage response, ref HttpClient client, ref string result, ref List<String> receiptList)
        {
            string ticketsGetEndPoint = Globals.CB_API.TICKETS_GET;
            string ticketsGetParams = String.Format(Globals.CB_API.TICKETS_GET_PARAMS, receiptList[0]);
            for (int i = 1; i < receiptList.Count; i++)
            {
                ticketsGetParams += String.Format("&receipt=" + receiptList[i]);
            }

            string ticketsGetURL = Globals.CB_API.CLICKBANK_API_URL + ticketsGetEndPoint + ticketsGetParams;

            client.Dispose();
            client = CreateFreshHttpClient();

            request = new()
            {
                RequestUri = new(ticketsGetURL),
                Method = HttpMethod.Get,
            };

            response = client.GetAsync(new Uri(ticketsGetURL)).Result;
            TicketResponse ticketGetResponse = response.Content.ReadFromJsonAsync<TicketResponse>().Result;
            // TicketResponse ticketGetResponse = null;
            // string sss = response.Content.ReadAsStringAsync().Result;

            if (response != null && ticketGetResponse != null && response.IsSuccessStatusCode)
            {   //for every ticket we found for all listed receipts (so for each active, recurring order for this person/email)
                foreach (var d in ticketGetResponse.ticketData)
                {
                    if (d.type == "CANCEL" || d.type == "REFUND")
                    {
                        switch (d.status)
                        {
                            case "OPENED":
                                // foundTicket = true;
                                // openTicket = true;
                                receiptList.Remove(d.receipt);
                                //safe to remove the receipt from the list as it's got an open cancel ticket
                                break;
                            case "REOPENED":
                                // foundTicket = true;
                                // openTicket = true;
                                receiptList.Remove(d.receipt);
                                //safe to remove the receipt from the list as it's got an open cancel ticket
                                break;
                            case "CLOSED":
                                // foundTicket = true;
                                // openTicket = false;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            else
            {
                if (response == null)
                {
                    Console.WriteLine("ticket get API response null");
                    result = "FAIL";
                }
                else
                {
                    if (response.IsSuccessStatusCode)
                    {
                        //OK no ticket
                    }
                    else
                    {
                        Console.WriteLine("ticket get API failure");
                        result = "FAIL";
                    }
                }
            }

            return true;
        }

        public static bool GetOrders2ActiveRecurring(ref APIUserInfo userInfo, ref OrderResponse orderResponse, ref HttpRequestMessage request, ref HttpResponseMessage response, ref HttpClient client, ref string result, ref List<String> receiptList)
        {
            client = CreateFreshHttpClient();

            DateTime startDate = DateTime.UtcNow.AddYears(-20).Date;
            int day = startDate.Day;
            string strday = "";
            if (day < 10)
            {
                strday += "0";
            }
            strday += day.ToString();
            int month = startDate.Month;
            string strmonth = "";
            if (month < 10)
            {
                strmonth += "0";
            }
            strmonth += month.ToString();
            int year = startDate.Year;
            string stryear = year.ToString();
            string startDatestr = stryear + "-" + strmonth + "-" + strday;

            DateTime endDate = DateTime.UtcNow.Date;
            day = endDate.Date.Day;
            strday = "";
            if (day < 10)
            {
                strday += "0";
            }
            strday += day.ToString();
            month = endDate.Month;
            strmonth = "";
            if (month < 10)
            {
                strmonth += "0";
            }
            strmonth += month.ToString();
            year = endDate.Year;
            stryear = year.ToString();
            string endDatestr = stryear + "-" + strmonth + "-" + strday;

            string orders2EndPoint = Globals.CB_API.ORDERS2_GET;
            string orders2GetParams = String.Format(Globals.CB_API.ORDERS2_GET_PARAMS, Globals.CB_API.CURRENT_VENDOR, userInfo.email, userInfo.zip, startDatestr, endDatestr); //lastName???
            if (userInfo.lastName == "testsale")
            {
                orders2GetParams = String.Format(Globals.CB_API.ORDERS2_GET_PARAMS_TEST, Globals.CB_API.CURRENT_VENDOR, userInfo.email, startDatestr, endDatestr); //lastName???
            }

            string orders2GetURL = Globals.CB_API.CLICKBANK_API_URL + orders2EndPoint + orders2GetParams;
            request = new()
            {
                RequestUri = new(orders2GetURL),
                Method = HttpMethod.Get,
            };

            response = client.GetAsync(new Uri(orders2GetURL)).Result;
            orderResponse = response.Content.ReadFromJsonAsync<OrderResponse>().Result;

            bool foundOrder = false;
            if (response != null && orderResponse != null && response.IsSuccessStatusCode)
            {
                foreach (var d in orderResponse.orderData)
                {
                    if (d.lineItemData.recurring == "true" && d.lineItemData.productTitle.Contains(Globals.CB_API.COMMON_PRODUCT_TITLE_STRING))
                    {
                        string status = "";
                        try
                        {
                            status = d.lineItemData.status.ToString();
                        }
                        catch (Exception e)
                        {
                            //API is giving us terrible values when certain strings are null - unfortunately status is one of those strings
                            //this is an easy way to clean them - otherwise we need custom get/set for dedicated models
                        }
                        if (!String.IsNullOrEmpty(status))
                        {
                            switch (status)
                            {
                                case "ACTIVE":
                                    receiptList.Add(d.receipt);
                                    foundOrder = true;
                                    break;
                                case "CANCELED":
                                    foundOrder = true;
                                    break;
                                default:
                                    break;
                            }
                        }


                    }

                }
            }
            else
            {
                if (response == null)
                {
                    Console.WriteLine("order GET api response null");
                    result = "FAIL";
                }
                else
                {
                    if (response.IsSuccessStatusCode)
                    {
                        //no order found for this customer
                        result = "NOT FOUND";
                    }
                    else
                    {
                        Console.WriteLine("order GET api fail");
                        result = "FAIL";
                    }
                }
            }

            if (foundOrder)
            {
                if (receiptList.Count == 0)
                {
                    result = "ALREADY CANCL";
                }
            }

            return true;
        }

        public static bool GetOrders2ActiveRecurring_NotSKU(string notThisSKU, ref LineItemData orderLineItemData, ref APIUserInfo userInfo, ref OrderResponse orderResponse, ref HttpRequestMessage request, ref HttpResponseMessage response, ref HttpClient client, ref string result, ref List<String> receiptList, ref string receipt)
        {
            bool foundNewSKU = false;

            DateTime startDate = DateTime.UtcNow.AddYears(-20).Date;
            int day = startDate.Day;
            string strday = "";
            if (day < 10)
            {
                strday += "0";
            }
            strday += day.ToString();
            int month = startDate.Month;
            string strmonth = "";
            if (month < 10)
            {
                strmonth += "0";
            }
            strmonth += month.ToString();
            int year = startDate.Year;
            string stryear = year.ToString();
            string startDatestr = stryear + "-" + strmonth + "-" + strday;

            DateTime endDate = DateTime.UtcNow.Date;
            day = endDate.Date.Day;
            strday = "";
            if (day < 10)
            {
                strday += "0";
            }
            strday += day.ToString();
            month = endDate.Month;
            strmonth = "";
            if (month < 10)
            {
                strmonth += "0";
            }
            strmonth += month.ToString();
            year = endDate.Year;
            stryear = year.ToString();
            string endDatestr = stryear + "-" + strmonth + "-" + strday;

            string orders2EndPoint = Globals.CB_API.ORDERS2_GET;
            string orders2GetParams = String.Format(Globals.CB_API.ORDERS2_GET_PARAMS, Globals.CB_API.CURRENT_VENDOR, userInfo.email, userInfo.zip, startDatestr, endDatestr); //lastName???
            if (userInfo.lastName == "testsale")
            {
                orders2GetParams = String.Format(Globals.CB_API.ORDERS2_GET_PARAMS_TEST, Globals.CB_API.CURRENT_VENDOR, userInfo.email, startDatestr, endDatestr); //lastName???
            }

            string orders2GetURL = Globals.CB_API.CLICKBANK_API_URL + orders2EndPoint + orders2GetParams;
            request = new()
            {
                RequestUri = new(orders2GetURL),
                Method = HttpMethod.Get,
            };

            client = CreateFreshHttpClient();
            response = client.GetAsync(new Uri(orders2GetURL)).Result;
            orderResponse = response.Content.ReadFromJsonAsync<OrderResponse>().Result;

            bool foundOrder = false;
            bool orderAlready19 = false;
            if (response != null && orderResponse != null && response.IsSuccessStatusCode)
            {
                foreach (var d in orderResponse.orderData)
                {
                    if (orderLineItemData == null && d.lineItemData.recurring == "true" && d.lineItemData.productTitle.Contains(Globals.CB_API.COMMON_PRODUCT_TITLE_STRING))
                    {
                        string status = "";
                        try
                        {
                            status = d.lineItemData.status.ToString();
                        }
                        catch (Exception e)
                        {
                            //API is giving us terrible values when certain strings are null - unfortunately status is one of those strings
                            //this is an easy way to clean them - otherwise we need custom get/set for dedicated models
                        }
                        if (!String.IsNullOrEmpty(status))
                        {
                            switch (status)
                            {
                                case "ACTIVE":
                                    foundOrder = true;
                                    receiptList.Add(d.receipt);
                                    if (d.lineItemData.itemNo != notThisSKU)
                                    {
                                        foundNewSKU = true;
                                        receipt = d.receipt;
                                        orderLineItemData = d.lineItemData;
                                    }
                                    else
                                    {
                                        orderAlready19 = true;
                                    }
                                    break;
                                case "CANCELED":
                                    foundOrder = true;
                                    break;
                                default:
                                    break;
                            }
                        }


                    }

                }
            }
            else
            {
                if (response == null)
                {
                    Console.WriteLine("order GET api response null");
                    result = "FAIL";
                }
                else
                {
                    if (response.IsSuccessStatusCode)
                    {
                        //no order found for this customer
                        result = "NOT FOUND";
                    }
                    else
                    {
                        Console.WriteLine("order GET api fail");
                        result = "FAIL";
                    }
                }
            }

            if (!foundOrder || !foundNewSKU)
            {
                result = "NOT FOUND";
            }
            if (orderAlready19 && !foundNewSKU)
            {
                result = "ALREADY 19";
            }

            return foundNewSKU;
        }




        public static bool PostOrders2ChangeOrder(string targetSKU, ref LineItemData orderLineItemData, ref HttpRequestMessage request, ref HttpResponseMessage response, ref HttpClient client, ref string result, ref List<String> receiptList, ref string receipt)
        {
            // receipt = receiptList[0];
            // receipt is set to order.receipt where order.active, .recurring, and .itemNo != notThisSKU
            receiptList.Remove(receipt);
            //remove from list so we can still keep a count in case there are multiple orders for above conditions (we're only operating on one per call)

            string oldSKU = orderLineItemData.itemNo;
            string newSKU = targetSKU;

            string orders2PostChangeEndPoint = String.Format(Globals.CB_API.ORDERS2_POST_CHANGEPRODUCT, receipt);
            string orders2PostChangePostParams = String.Format(Globals.CB_API.ORDERS2_POST_CHANGEPRODUCT_PARAMS, oldSKU, newSKU);
            string orders2ChangePostURL = Globals.CB_API.CLICKBANK_API_URL + orders2PostChangeEndPoint + orders2PostChangePostParams;

            client.Dispose();
            client = CreateFreshHttpClient();

            request = new()
            {
                RequestUri = new(orders2ChangePostURL),
                Method = HttpMethod.Post,
            };

            response = client.SendAsync(request).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.

            if (response != null && response.IsSuccessStatusCode) //do we care about ticketpostresponse here?
            {
                result = "OK";
            }
            else
            {
                if (response == null)
                {
                    Console.WriteLine("orders2 changeOrder post API null response");
                    result = "FAIL";
                }
                else
                {
                    Console.WriteLine("orders2 changeOrder post API fail");
                    result = "FAIL";
                }
            }

            return true;
        }



        // public Task<IEnumerable<Contact>> GetAll() => Task.FromResult(_contacts.AsEnumerable());

        // public Task<Contact> Get(int id) => Task.FromResult(_contacts.FirstOrDefault(x => x.ContactId == id));

        // public Task<int> Add(Contact contact)
        // {
        //     var newId = (_contacts.LastOrDefault()?.ContactId ?? 0) + 1;
        //     _contacts.Add(new Contact(newId, contact.Name, contact.Address, contact.City));
        //     return Task.FromResult(newId);
        // }

        // public async Task Delete(int id)
        // {
        //     var contact = await Get(id);
        //     if (contact == null)
        //     {
        //         throw new InvalidOperationException(string.Format("Contact with id '{0}' does not exists", id));
        //     }

        //     _contacts.Remove(contact);
        // }
    
    
    }
}

