package com.kamensek.gasper.gkstepcounter;

import android.app.Activity;
import android.location.Location;
import android.widget.Toast;

import org.json.JSONObject;

import java.io.BufferedInputStream;
import java.io.DataOutputStream;
import java.io.InputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

/**
 * Created by RaspeR87 on 13/04/2017.
 */

public class MyThread extends Thread {

    private int steps;
    private int resetSteps;
    private int allTimeSteps;
    private Date timestampReset;
    private Date firstTimestamp;
    private Activity activity;
    private String request;

    public MyThread(int steps, int resetSteps, int allTimeSteps, Date timestampReset, Date firstTimestamp, Activity activity, String request)
    {
        this.steps = steps;
        this.resetSteps = resetSteps;
        this.allTimeSteps = allTimeSteps;
        this.timestampReset = timestampReset;
        this.firstTimestamp = firstTimestamp;
        this.activity = activity;
        this.request = request;
    }

    @Override
    public void run() {
        try {
            URL url = new URL(request);
            HttpURLConnection connection = (HttpURLConnection) url.openConnection();
            connection.setDoOutput(true);
            connection.setDoInput(true);
            connection.setRequestMethod("POST");
            connection.setRequestProperty("Content-Type", "application/json");
            connection.setRequestProperty("charset", "utf-8");

            DataOutputStream wr = new DataOutputStream(connection.getOutputStream());

            JSONObject jsonParam = new JSONObject();

            SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss'Z'", Locale.US);
            sdf.setTimeZone(TimeZone.getTimeZone("Etc/GMT-2"));

            jsonParam.put("Timestamp", sdf.format(new Date()));
            jsonParam.put("Steps", steps);
            jsonParam.put("ResetSteps", resetSteps);
            jsonParam.put("All Time Steps", allTimeSteps);
            jsonParam.put("TimestampReset", sdf.format(timestampReset));
            jsonParam.put("FirstTimestamp", sdf.format(firstTimestamp));

            wr.writeBytes("[" + jsonParam.toString() + "]");

            wr.flush();
            wr.close();

            InputStream is = new BufferedInputStream(connection.getInputStream());
            is.close();
        }
        catch (Exception _ex) {
            Toast.makeText(activity, _ex.getMessage(),
                    Toast.LENGTH_LONG).show();
        }
    }
}
