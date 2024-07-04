#!/bin/sh

set -ex

# This script will regenerate the `wit-bindgen`-generated files in this
# directory.

# Prerequisites:
#   POSIX shell
#   tar
#   [cargo](https://rustup.rs/)
#   [curl](https://curl.se/download.html)

cargo install --locked --no-default-features --features csharp --version 0.27.0 wit-bindgen-cli
curl -OL https://github.com/WebAssembly/wasi-http/archive/refs/tags/v0.2.0.tar.gz
tar xzf v0.2.0.tar.gz
cat >wasi-http-0.2.0/wit/world.wit <<EOF
world wasi-http {
  import outgoing-handler;
}
EOF
wit-bindgen c-sharp -w wasi-http -r native-aot --internal wasi-http-0.2.0/wit
rm -r wasi-http-0.2.0 v0.2.0.tar.gz
