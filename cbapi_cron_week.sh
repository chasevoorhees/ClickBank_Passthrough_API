#!/bin/sh
SERVICE='cbapiserviceconsole'

sudo pkill cbapiservicecon
sudo pkill cbapiserviceconsole
sudo nohup /home/ubuntu/Release_Ubu64/cbapiserviceconsole &

