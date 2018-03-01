#!/bin/bash
set -e

url=`cat deps.url`

rm bin -fr
mkdir -p ./bin/nuget
cd ./bin/nuget

curl $url -O -L
