#!/bin/bash

openssl pkcs12 -inkey localhost.key -in localhost.crt -export -out localhost.pfx