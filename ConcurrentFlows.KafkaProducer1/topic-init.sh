#!/bin/bash
# set -x

echo -e "\nStarting Topic Init... Waiting for Rest Proxy"
while [ "`curl -s http://rest-proxy:8082/brokers`" != "{\"brokers\":[1,2,3]}" ]
do 
    echo -e $(date) "Waiting for all three Brokers"
    sleep 5
done  

echo -e "\nCreating topics"

kafka-topics --bootstrap-server broker1:29092,broker2:29093,broker3:29094 --delete --if-exists --topic sync_topic
kafka-topics --bootstrap-server broker1:29092,broker2:29093,broker3:29094 --create --if-not-exists --topic sync_topic --replication-factor 3 --partitions 10

kafka-topics --bootstrap-server broker1:29092,broker2:29093,broker3:29094 --delete --if-exists --topic async_topic
kafka-topics --bootstrap-server broker1:29092,broker2:29093,broker3:29094 --create --if-not-exists --topic async_topic --replication-factor 3 --partitions 10

echo -e "\nAll Topics"
kafka-topics --bootstrap-server broker1:29092 --list