package com.kamensek.gasper.gkmobilesensors;

import android.app.Activity;
import android.location.Address;
import android.location.Geocoder;
import android.location.Location;
import android.location.LocationListener;
import android.os.Bundle;

import org.json.JSONObject;

import java.io.DataOutputStream;
import java.io.IOException;
import java.net.HttpURLConnection;
import java.net.URL;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;

/**
 * Created by gasperr on 15. 03. 2017.
 */

public class MyLocationListener implements LocationListener {

    private String request;
    private Activity activity;

    public MyLocationListener(Activity activity, String request)
    {
        this.activity = activity;
        this.request = request;
    }

    @Override
    public void onLocationChanged(Location loc) {
        new MyThread(loc, activity, request).start();
    }

    @Override
    public void onProviderDisabled(String provider) {}

    @Override
    public void onProviderEnabled(String provider) {}

    @Override
    public void onStatusChanged(String provider, int status, Bundle extras) {}
}
