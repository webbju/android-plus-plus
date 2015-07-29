#!/system/bin/sh

# This script launches lldb-server on Android device from application subfolder - /data/data/$packageId/lldb/bin.
# Native run configuration is expected to push this script along with lldb-server to the device prior to its execution.
# Following command argument are expected to be passed - package name and lldb-server listen port.

PACKAGE_DIR=/data/data/$1
LLDB_DIR=$PACKAGE_DIR/lldb
BIN_DIR=$LLDB_DIR/bin

LOG_DIR=$LLDB_DIR/log
TMP_DIR=$LLDB_DIR/tmp

export LLDB_DEBUGSERVER_LOG_FILE=$LOG_DIR/llgs.log
export LLDB_SERVER_LOG_CHANNELS="lldb all:gdb-remote packets:linux ptrace"

rm -rf $TMP_DIR
mkdir -p $TMP_DIR
export TMPDIR=$TMP_DIR

rm -rf $LOG_DIR
mkdir -p $LOG_DIR

cd $TMP_DIR # change cwd

# lldb-server platform exits after debug session has been completed.
$BIN_DIR/lldb-server platform --listen *:$2 -c "log enable -Tp -f $LOG_DIR/platform-gdb-remote.log gdb-remote all" -c "log enable -Tp -f $LOG_DIR/platform-lldb.log lldb all" </dev/null >$LOG_DIR/platform-stdout.log 2>&1
