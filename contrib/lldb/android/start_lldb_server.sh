#!/system/bin/sh

# This script launches lldb-server on Android device from application subfolder - /data/data/$packageId/lldb/bin.
# Native run configuration is expected to push this script along with lldb-server to the device prior to its execution.
# Following command arguments are expected to be passed - lldb package directory and lldb-server listen port.

LLDB_DIR=$1
BIN_DIR=$LLDB_DIR/bin

LOG_DIR=$LLDB_DIR/log
TMP_DIR=$LLDB_DIR/tmp

export LLDB_DEBUGSERVER_LOG_FILE=$LOG_DIR/gdb-server.log
export LLDB_SERVER_LOG_CHANNELS="$3"

rm -r $TMP_DIR
mkdir $TMP_DIR
export TMPDIR=$TMP_DIR

rm -r $LOG_DIR
mkdir $LOG_DIR

cd $TMP_DIR # change cwd

# Send SIGTERM for all spawned processes.
#trap "kill 0" SIGINT SIGTERM EXIT

# lldb-server platform exits after debug session has been completed.
$BIN_DIR/lldb-server platform --listen *:0 --log-file $LOG_DIR/platform.log --log-channels "$3" --port-file $2 </dev/null >$LOG_DIR/platform-stdout.log 2>&1
