This folder contains sub-directories per each Android ABI - x86, armeabi, mips,...
Each of ABI directory includes lldb-server binary which is cross-compiled for this architecture.
Please see cross-compilation instructions here -
https://docs.google.com/a/google.com/document/d/1Wox_0rlD-5UKn4Fq55SwEqRLeMwhXCu1f6FBGHlAYvM/edit?usp=sharing

Size of lldb-server binary is critical aspect since it affects user debugging experience - please use MinSizeRel build configuration
with stripped debug symbols - i.e., "-DCMAKE_BUILD_TYPE=MinSizeRel -DCMAKE_CXX_FLAGS="${CMAKE_CXX_FLAGS} -s""
