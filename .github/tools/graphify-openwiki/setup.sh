#!/bin/bash
set -e

graphify . --code-only
graphify claude install
graphify opencode install
openwiki code --init --print
