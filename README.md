# Android++ (android-plus-plus)

Android++ is a freely distributed extension and associated MSBuild scripts designed to enable
Android application development within Visual Studio. Primarily for NDK based C/C++ applications,
it also incorporates customisable deployment, resource management, and integrated Java source compilation.

---

### Getting Started

##### Prerequisites:

- **Android SDK** (http://developer.android.com/sdk/)
  * Please avoid installing to a path which contains spaces.

- **Android NDK** (https://developer.android.com/tools/sdk/ndk/)
  * Please avoid installing to a path which contains spaces.
  * Compatibility tested with NDK r9d, r10, r10b, r10c and r10d.

- **Java Development Kit (JDK) 1.7 or 1.8**
  * If using JDK 1.7, please ensure you are using a revision of at least 67.
  * If using JDK 1.8, please ensure you are using a revision of at least 21.
  * Early revisions of JDK 1.7 tend to cause devices to hang when debugging.

##### Installation:

1. Create several new environment variables to reference installations of the SDK, NDK, and JDK.

   * Open your computer's 'Control Panel'.
     * If viewing items by category, follow 'System and Security' -> 'System'.
     * If viewing items by icon, just click 'System'.
   * Click 'Advanced System Settings' (left panel). This should open a 'System Properties'.
   * Select the 'Advanced' tab, and click 'Environment Variables'.
   * Add the following new user or system variables:
     * name: `ANDROID_SDK`, value: `<path_to_sdk_root>`
     * name: `ANDROID_NDK`, value: `<path_to_ndk_root>`
     * name: `JAVA_HOME`, value: `<path_to_java_jdk_root>`

2. Close all instances of Visual Studio and any Command Prompt windows.

3. Bootstrap for one (or more) instances of Visual Studio using the `bootstrap_vs*.cmd` scripts in Android++'s root.
   * Scripts are separated by version to allow for improved customisation/testing.
   * More advanced scripts can be found in the `./bootstrap/` directory. These allow more fine grain control.
   * Installation of Visual Studio extensions in is a little unpredictable, be sure to uninstall any existing registered extensions before upgrading. Look in `./bootstrap/` for these scripts.
   * Improved automation of the upgrade process will follow in later releases.

4. Build a sample. See below.

##### Building and running 'hello-gdbserver' sample:

1. Find bundled projects located under `msbuild/samples` from the root of your Android++ installation.

2. Build the `hello-gdbserver` project. This is a tiny application to force a segmentation fault.

3. Ensure 'hello-gdbserver' is set as the launch project. This is indicated by the project name being represented in bold.
   * If it's not bold, right-click the project in the 'Solution Explorer' pane and select 'Set as StartUp Project'.

4. Run the project. Press F5 or select 'Debug -> Start Debugging'.

5. A 'Configuring Android++' dialog should appear. Installation and connection status can be monitored here.
   * A 'Waiting for Debugger' prompt should also appear on the device. When this disappears, JDB has successfully connected.
   * As the ADB protocol is slow, installation times can be lengthy for large APKs - and vary with target device.
   * If you experience any errors, please first consult `./docs/troubleshooting.txt`.

6. Wait for connection to be finalised. On device you should see a large button labelled 'Induce Crash'. Press it.

7. Visual Studio should alert you that a 'Segmentation Fault' has occurred. Click 'Break' to see its location in native code.

---

### Troubleshooting

1. **The project built successfully, but nothing happens when I try running it.**

  This issue is likely due to only one part of Android++ being installed correctly.
  To validate this open Visual Studio and navigate to 'Tools -> Extension Manager'. When installed correctly, there will
  be a Android++ listing here - if this is missing you should attempt to reinstall the extension by bootstrapping again.

2. **"No device/emulator found or connected. Check status via 'adb devices'."**

  This error is produced when ADB (Android Debug Bridge) can not locate any attached devices or emulators.

  For USB debugging to work it must be enabled under the device's 'Developer options' page located
  under 'Settings' - if this is missing see (2a).

  Ensure that the 'USB debugging' option is ticked under this page. On recent devices, when this is ticked and a
  cable connected - you will be prompted to confirm that the device should be allowed to white-list the pairing.
  USB debugging is often complicated on Android by the requirement for your desktop to have the appropriate USB drivers
  installed. This can be troublesome.

  For more information on using your device for development:
    http://developer.android.com/tools/device.html

  a) You can enable 'Developer options' by pressing 7 times on the 'Build number' text displayed at the bottom
  of the device's 'Settings -> About Phone' page.

3. **"Can not debug native code on this device/emulator."**

  With the release of Android 4.3, an oversight lead to the inability to debug native code.

  When configuring a target for debugging, the 'run-as' tool is used in order to spawn the GDBserver for a particular
  application. This tool takes a package address as input, in order for a particular command to be run under correct
  application permissions. A number of changes to this tool (and the file-system) have lead to errors whereby package
  addresses will not be recognised. This means we can't achieve native debugging.

  Although this issue is patched in 4.4, a number of custom distributions still suffer from the issue. As of Sept 2014,
  Samsung appear to not have updated/corrected this issue on a number of their phones & distributions.

  More info: https://code.google.com/p/android/issues/detail?id=58373
