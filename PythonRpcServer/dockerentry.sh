#!/bin/bash
# Cant modify read-only /etc/hosts in Dockerfile
grep www.youtube.com /etc/hosts || echo "172.217.0.46 www.youtube.com"  >> /etc/hosts

nice -n 18  ionice -c 2 -n 6  python3  -u  /PythonRpcServer/server.py