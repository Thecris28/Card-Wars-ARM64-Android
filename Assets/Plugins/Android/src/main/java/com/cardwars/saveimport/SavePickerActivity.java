package com.cardwars.saveimport;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.util.Base64;

import com.unity3d.player.UnityPlayer;

import java.io.ByteArrayOutputStream;
import java.io.InputStream;

public final class SavePickerActivity extends Activity {
    private static final int REQUEST_OPEN_DOCUMENT = 8401;
    private static final int MAX_SAVE_BYTES = 1024 * 1024;
    private static final String UNITY_OBJECT = "CardWarsProfileManager";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
        intent.addCategory(Intent.CATEGORY_OPENABLE);
        intent.setType("*/*");
        intent.putExtra(
            Intent.EXTRA_MIME_TYPES,
            new String[] { "application/json", "text/plain" }
        );
        intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
        try {
            startActivityForResult(intent, REQUEST_OPEN_DOCUMENT);
        } catch (Exception exception) {
            sendError(exception.getMessage());
            finish();
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode != REQUEST_OPEN_DOCUMENT) {
            return;
        }
        if (resultCode != RESULT_OK || data == null || data.getData() == null) {
            UnityPlayer.UnitySendMessage(
                UNITY_OBJECT,
                "OnSaveFilePickerCancelled",
                ""
            );
            finish();
            return;
        }

        try {
            String encodedJson = readSelectedFile(data.getData());
            UnityPlayer.UnitySendMessage(
                UNITY_OBJECT,
                "OnSaveFilePicked",
                encodedJson
            );
        } catch (Exception exception) {
            sendError(exception.getMessage());
        }
        finish();
    }

    private String readSelectedFile(Uri uri) throws Exception {
        try (InputStream input = getContentResolver().openInputStream(uri);
             ByteArrayOutputStream output = new ByteArrayOutputStream()) {
            if (input == null) {
                throw new IllegalStateException("No se pudo abrir el archivo seleccionado.");
            }
            byte[] buffer = new byte[8192];
            int total = 0;
            int count;
            while ((count = input.read(buffer)) != -1) {
                total += count;
                if (total > MAX_SAVE_BYTES) {
                    throw new IllegalArgumentException("El archivo supera el límite de 1 MB.");
                }
                output.write(buffer, 0, count);
            }
            return Base64.encodeToString(output.toByteArray(), Base64.NO_WRAP);
        }
    }

    private void sendError(String message) {
        String details = message == null ? "Error al leer el archivo." : message;
        String encoded = Base64.encodeToString(
            details.getBytes(java.nio.charset.StandardCharsets.UTF_8),
            Base64.NO_WRAP
        );
        UnityPlayer.UnitySendMessage(
            UNITY_OBJECT,
            "OnSaveFilePickerError",
            encoded
        );
    }
}
