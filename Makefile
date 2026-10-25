SHELL := bash
.DEFAULT_GOAL := help

ROOT := $(patsubst %/,%,$(dir $(abspath $(lastword $(MAKEFILE_LIST)))))
export ROOT
CONFIGURATION ?= Release
export CONFIGURATION

include $(ROOT)/make/common.mk
include $(ROOT)/make/lib.mk
include $(ROOT)/make/api.mk
include $(ROOT)/make/gate.mk
include $(ROOT)/make/host.mk
include $(ROOT)/make/store.mk
include $(ROOT)/make/admin.mk
include $(ROOT)/make/infra.mk
include $(ROOT)/make/ci.mk
