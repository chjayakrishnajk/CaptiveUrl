#!/bin/bash

# List of captive portal check URLs
urls=(
  "http://www.google.com/generate_204"
  "http://captive.apple.com"
  "http://www.msftconnecttest.com/connecttest.txt"
  "http://www.facebook.com/generate_204"
  "http://www.cloudflare.com/cdn-cgi/trace"
)

# Loop through the URLs and curl each one
for url in "${urls[@]}"; do
  echo "Checking URL: $url"
  response=$(curl -s -w "\n%{http_code}" "$url")
  content=$(echo "$response" | sed '$d')
  code=$(echo "$response" | tail -n1)
  echo "Response Code: $code"
  echo "Response Content: $content"
  echo
done
