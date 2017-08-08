package com.kamensek.gasper.gkmobilesensors;

import android.app.Activity;
import android.location.Location;
import android.widget.Toast;

import org.json.JSONObject;

import java.io.BufferedInputStream;
import java.io.ByteArrayOutputStream;
import java.io.DataOutputStream;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.Reader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

/**
 * Created by gasperr on 15. 03. 2017.
 */

public class MyThread extends Thread {

    private Location loc;
    private Activity activity;
    private String request;

    public MyThread(Location loc, Activity activity, String request)
    {
        this.loc = loc;
        this.activity = activity;
        this.request = request;
    }

    @Override
    public void run() {
        try {
            // Calculate longitude/latitude
            double _longitude = loc.getLongitude();
            double _latitude = loc.getLatitude();
            double _elevation = loc.getAltitude();
            double _speed = loc.getSpeed();
            double _elevationAlLTimeHigh = 0.0;
            double _elevationAllTimeLow = 0.0;

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

            jsonParam.put("TimeStamp", sdf.format(new Date()));
            jsonParam.put("Longitude", _longitude);
            jsonParam.put("Latitude", _latitude);
            jsonParam.put("Elevation", _elevation);
            jsonParam.put("Speed", _speed);
            jsonParam.put("ElevationAllTimeHigh", _elevationAlLTimeHigh);
            jsonParam.put("ElevationAllTimeLow", _elevationAllTimeLow);

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
