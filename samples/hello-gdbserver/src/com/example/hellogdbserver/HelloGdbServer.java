/*
 * Copyright (C) 2010 Max Vilimpoc
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */
package com.example.hellogdbserver;

import android.app.Activity;
import android.os.Bundle;
import android.view.View;
import android.view.View.OnClickListener;
import android.widget.Button;

public class HelloGdbServer extends Activity {
	static {
		System.loadLibrary("hello-gdbserver");
	}
	
	private OnClickListener induceCrashListener = new OnClickListener() {
		// When the button is clicked, induce a crash via the native code.
		public void onClick(View v) {
			invokeCrash();
		}
	};
	
    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.main);
        
        Button button = (Button) findViewById(R.id.induceCrashButton);
        button.setOnClickListener(induceCrashListener);
    }

    public static native void invokeCrash();
}