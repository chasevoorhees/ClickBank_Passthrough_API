#!/bin/sh

# Used in production to restart service to ensure pem keys used are up to date

SERVICE='cbapiserviceconsole'
 
sudo pkill cbapiservicecon
sudo pkill cbapiserviceconsole
sudo /home/ubuntu/Release_Ubu64/cbapiserviceconsole
