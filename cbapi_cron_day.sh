#!/bin/sh
SERVICE='abapiservicecon'
 
if ps ax | grep -v grep | grep $SERVICE > /dev/null
then
    echo "$SERVICE service running, everything is fine"
else
    echo "$SERVICE is not running, starting"
    sudo /home/ubuntu/Release_Ubu64/cbapiserviceconsole
fi