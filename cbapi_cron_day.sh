#!/bin/sh

# Used in production to restart service if it crashes (not that I've seen that issue) or is leaking mem (nor this)

SERVICE='abapiservicecon'
 
if ps ax | grep -v grep | grep $SERVICE > /dev/null
then
    echo "$SERVICE service running, everything is fine"
else
    echo "$SERVICE is not running, starting"
    sudo /home/ubuntu/Release_Ubu64/cbapiserviceconsole
fi