package com.cardwars.saveimport;

import android.app.Activity;
import android.content.Intent;

public final class SavePicker {
    private SavePicker() {
    }

    public static void launch(Activity activity) {
        activity.startActivity(new Intent(activity, SavePickerActivity.class));
    }
}
