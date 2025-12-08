#!/bin/bash

PARENT="topaz"
SUFFIX=".local.dev"
openssl req \
-x509 \
-newkey rsa:4096 \
-sha256 \
-days 365 \
-nodes \
-keyout $PARENT.key \
-out $PARENT.crt \
-subj "/CN=${PARENT}" \
-extensions v3_ca \
-extensions v3_req \
-config <( \
  echo '[req]'; \
  echo 'default_bits= 4096'; \
  echo 'distinguished_name=req'; \
  echo 'x509_extension = v3_ca'; \
  echo 'req_extensions = v3_req'; \
  echo '[v3_req]'; \
  echo 'basicConstraints = CA:FALSE'; \
  echo 'keyUsage = nonRepudiation, digitalSignature, keyEncipherment'; \
  echo 'subjectAltName = @alt_names'; \
  echo '[ alt_names ]'; \
  echo "DNS.1 = www.${PARENT}${SUFFIX}"; \
  echo "DNS.2 = ${PARENT}${SUFFIX}"; \
  echo "DNS.3 = *.${PARENT}${SUFFIX}"; \
  echo "DNS.4 = *.table.storage.${PARENT}${SUFFIX}"; \
  echo "DNS.5 = *.blob.storage.${PARENT}${SUFFIX}"; \
  echo "DNS.6 = *.queue.storage.${PARENT}${SUFFIX}"; \
  echo "DNS.7 = *.servicebus.${PARENT}${SUFFIX}"; \
  echo "DNS.8 = *.eventhub.${PARENT}${SUFFIX}"; \
  echo "DNS.9 = *.keyvault.${PARENT}${SUFFIX}"; \
  echo '[ v3_ca ]'; \
  echo 'subjectKeyIdentifier=hash'; \
  echo 'authorityKeyIdentifier=keyid:always,issuer'; \
  echo 'basicConstraints = critical, CA:TRUE, pathlen:0'; \
  echo 'keyUsage = critical, cRLSign, keyCertSign'; \
  echo 'extendedKeyUsage = serverAuth, clientAuth')

openssl x509 -noout -text -in $PARENT.crt