# ClickBank-Passthrough-API
Simple pass-through API to allow Javascript API calls from users to create cancellation request tickets for ClickBank subscription orders.

Why? To access the Clickbank API you must use your API and Developer keys. These keys are basically superusers on your Clickbank account. When running code serverside, that's not a problem.

However, Javascript clients like the ones included in client*.html run *client-side* in every user's browser. We cannot put the API keys into the JS code because then any user could use them to modify
data within the API.

So - We use this passthrough API to let the client JS call it instead. This API then makes the ClickBank API call safely from its server and returns the relevant data to the client. This API also does a fair amount of work that we'd otherwise need to run (slowly) on the user's page in JS.

It's not perfect: Currenly we just log IP attempts to use the API and disable IPs after 5 calls within 24hrs (minus when IP=localhost or last_name = testsale). Other methods like footprinting or cookies could be employed, but wouldn't realistically add much security. However, minifying and obfuscating the JS client would *definitely* be a good idea.




----------------------
Args (**required)
======================

Example:

sudo ./cbapiserviceconsole VENDOR=myvendorname MONTHLY_PRODUCT_SKU=11 CANCEL_PRODUCT_TITLE="Membership Subscription" CERT_MODE=PROD CERT_PEM=/home/ubuntu/cert.pem KEY_PEM=/home/ubuntu/cert.pem URL_LIST=https://APIDOMAIN.COM:5001; API_AUTHKEY=DEVKEY:APIKEY CORS_LIST=https://SITEDOMAIN.COM 



--------------------
CERT_MODE = DEV
--------------------
PROD (req CERT_PEM, KEY_PEM) 
DEV (req dotnet trust, see SSL Setup for DEV)
NONE (no https allowed in URL_LIST, https port not used)

--------------------
CERT_PEM     
--------------------                   
file loc

--------------------
KEY_PEM             
--------------------            
file loc

--------------------
HTTP_PORT = 5000    
--------------------              

--------------------
HTTPS_PORT = 5001
--------------------

--------------------
USER_AGENT = CB_API_Example1.0
--------------------

--------------------
URL_LIST = "http://127.0.0.1:5000;https://127.0.0.1:5001;"
--------------------
*semi-colon separated values
http://*:5000 https://*:5001
https://PUBLIC_URL:5001

--------------------
**API_AUTHKEY = DEV-KEY:API-KEY
--------------------
DEVKEY:APIKEY

--------------------
**VENDOR = jbitmedpro
--------------------
CB Vendor Name

--------------------
**CANCEL_PRODUCT_TITLE = Pain Free
--------------------
*Filters in CancelProduct for this order.product_title

--------------------
**MONTHLY_PRODUCT_SKU = 43
--------------------
*Order change function looks for an order without this SKU but with product title containing CANCEL_PRODUCT_TITLE, status = active, and recurring = true
then calls changeOrder to alter that order with currentorder.itemNo = oldSKU and newSKU = MONTHLY_PRODUCT_SKU

--------------------
CORS_LIST = https://SITEDOMAIN.com http://127.0.0.1 http://localhost https://localhost https://127.0.0.1
--------------------
*space separated values
URLs we can cross-origin call the pass-through API from (so the domain where you're hosting this)

--------------------
CORS = true
--------------------
true requires CORS_LIST
true/false


----------------------
How to use this:
======================
The API needs to be run on any server (currently compiled for Ubuntu in the Zip file). It's binary file can be run just by calling it from a terminal/console by name: cbapiserviceconsole
It runs on port 5001 (https). 
This port need to be opened/accessible via the virtual machine configuration. You NEED an Elastic/Static IP address assigned to the VM instance you're using, and a DNS A Record for cbapi.j3innovations.com pointing to it.

Follow SSL Setup instructions or SSL service will not bind and app will crash.

The client.html file contains both the HTML and JS for the submission form. The only field you need to update in here is var url. Currently it's pointing at localhost; it needs to point to the IP of the server/virtual machine.

Otherwise, you can just paste the contents of this file into a custom element on a ClickFunnels (or any) page.

It needs JQuery to run. This is already imported by ClickFunnels along with its other JS references. You need to uncomment the jquery.min*.js import to run the html file locally.

Testing:

Set last name = "testsale" in the form in order to ignore zip input and search for orders type=TEST_SALE. This will only look for subscriptions with given email and of type test_sale then submit cancellation tickets for them as usual (this will often result in return of OK MORE).


----------------------
Features this could use
======================
I ended up leaving out ZIP from the orders2 list call, so it's required by the form but technically not used. It could be used as an additional verification.

We can receive more than one active, recurring order when getting Orders2/list. A call will only change/cancel a single order at a time, but it will notify the user if more exist. However, for the page-foward-on-success options, we may want to add another dialogue.

I deployed this to AWS Micro so I wanted to keep everything as small as possible, but persistent logging may be a good idea. Currently, logs are in-mem and last only 24hrs. We may want to log at least "really bad" user info.

Lastly, the input sanitization is fairly minimal. 


----------------------
SSL Setup for DEV
======================
Windows:
https://docs.microsoft.com/en-us/dotnet/core/additional-tools/self-signed-certificates-guide

>dotnet dev-certs https --trust


Ubuntu:

https://github.com/BorisWilhelms/create-dotnet-devcert

>snap install dotnet-sdk --classic

>sudo apt install libnss3-tools

>./create-dotnet-devcert.sh
(same bash script is in project dir)

----------------------
Passthrough API Info
======================
Input Paramters (querystring): Email, Zip, and Last Name.

url = 'http://127.0.0.1:5000/cbapi/requestcancel';

Return: 200 "OK" 200 "OK MORE" 201 "ALREADY CANCL" 404 "NOT FOUND" 403 "BAD IP" 409 "REJECT"
(OK MORE is returned when we see multiple active subscriptions. We only POST/create one cancellation ticket per form submission so we let the user do so again.)
(REJECT is for when user IP has exceeded 24hr limit)

GETs orders2 API endpoint, finds matching order with product_title.contains(COMMON_PRODUCT_TITLE_STRING), recurring=true, and order.lineItemData.status = "ACTIVE".

Adds order.receipt string to receiptList array, then GETs tickets API endpoint with all receipt strings in array.

Parses returned tickets to find order receipt with missing or closed ticket, meaning the subscription is still ongoing and still needs a ticket to cancel.

POSTs tickets API endpoint with /1.3/tickets/{receipt}?type=cncl&reason=ticket.type.cancel.7&comment=CBAPICustomerCancel

Returns string and HTTP status with result which is then parsed by JS to update HTML form and provide error or confirmation data.


Input Paramters (querystring): Email, Zip, and Last Name.

url = 'http://127.0.0.1:5000/cbapi/requestchange19';

Return: 200 "OK" 200 "OK MORE" 201 "ALREADY 19" 404 "NOT FOUND" 403 "BAD IP" 409 "REJECT"
(OK MORE is returned when we see multiple active subscriptions. We only POST one changeOrder per form submission so we pause for a bit on the form to notify them they may want to contact support.)
(REJECT is for when user IP has exceeded 24hr limit)

GETs orders2 API endpoint, finds matching order with itemNo != CLICKBANK_MONTHLY_PRODUCT_SKU, product_title.contains(COMMON_PRODUCT_TITLE_STRING), recurring=true, and order.lineItemData.status = "ACTIVE".

Adds order.receipt string to receiptList array.

POSTs orders2/changeOrder API endpoint with oldSKU and newSKU (CLICKBANK_MONTHLY_PRODUCT_SKU)

Returns string and HTTP status with result which is then parsed by JS to update HTML form and provide error or confirmation data.


----------------------
Anti-Abuse Features
----------------------
Pretty simple - we store user IP every call and save it for every call that's not from localhost, run with testsale, or returns OK MORE.

Each call runs through the in-memory IP list and removes any that have been there for more than 24hrs. If a user tries more than 4 bad calls within 24hrs, we return REJECT
and tell them to try again later.



----------------------
ClickBank API Info/Quirks
======================

1. The JSON returned for null fields is very ugly.

status is a nullable string. When we generate a C# class (when the API value = null) directly from the returned JSON:

"status": {
    "@nil": "true"
},

This is what we end up with:

public class status
{
    public String Nil { get; set; } 
}

I tried making a reuseable get-set class but it didn't play nice with my deserializer, so we end up using Objects:

public Object status { get; set; }

To parse this, we're stuck with a rather inelegant try-catch solution:

string status = "";
try
{
    status = d.lineItemData.status.ToString();
}
catch (Exception e)
{
    //status is null object
}
if (!String.IsNullOrEmpty(status))
{
    //now check status==true/false


}



2. The API Requires a strangely formed/non-standard API Authorization header in format below - add it like this:

client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authKey);

Adding it to the HeaderList any other way caused automatic validation, but our string is not Base64 nor is it any of the correct 'Authorization' header types (Bearer/Basic/etc) which require you to prepend
your string with the type eg. Authorization: Bearer authKey

Likewise, the Header.Name Authorization is reserved so validation will always fail if we add it like a normal header.


All headers:

Authorization DEV-KEY:API-KEY
Accept application/json
page # (optional)
Connection keep-alive
User-Agent CB_API_Example1.0 

Notes: 
Accept-Encoding gzip, deflate, br will indeed zip the return body (not supported in this project)
We're not using page=# in any of our params; if you're working with many records you'll need to handle that


----------------------
HTML Clients
======================
You'll need to uncomment JQuery import if you want to test these locally in your browser (as well as have the JQuery.*.min.js file in the same folder as the html file):
    <!-- <script src="jquery-1.4.1.min.js" type="text/javascript"></script> -->

Also to change these strings in the HTML files to your own values:

APIDOMAIN
REPLACE_CANCEL_URL
VENDORNAME
ITEMNO
REPLACE_DOWNSELL_URL

Otherwise, you should be able to just copy-paste the entire file contents into a custom JS/HTML web element on a page of your choice. 




----------------------
changeOrder notes
======================
FYI You can't change a subscription order to a non-subscription. You need to cancel then forward the user to a Clickbank signup page.

Signup page format:
https://VENDORNAME.pay.clickbank.net/?cbitems=ITEMNO





