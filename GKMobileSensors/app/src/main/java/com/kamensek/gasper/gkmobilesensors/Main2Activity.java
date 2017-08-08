package com.kamensek.gasper.gkmobilesensors;

import android.Manifest;
import android.content.Context;
import android.location.LocationListener;
import android.location.LocationManager;
import android.support.design.widget.TabLayout;
import android.support.design.widget.FloatingActionButton;
import android.support.design.widget.Snackbar;
import android.support.v4.app.ActivityCompat;
import android.support.v7.app.AppCompatActivity;
import android.support.v7.widget.Toolbar;

import android.support.v4.app.Fragment;
import android.support.v4.app.FragmentManager;
import android.support.v4.app.FragmentPagerAdapter;
import android.support.v4.view.ViewPager;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;

import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;

import org.json.JSONObject;

import java.io.DataOutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.TimeZone;
import java.util.concurrent.ScheduledThreadPoolExecutor;
import java.util.concurrent.TimeUnit;

public class Main2Activity extends AppCompatActivity {

    /**
     * The {@link android.support.v4.view.PagerAdapter} that will provide
     * fragments for each of the sections. We use a
     * {@link FragmentPagerAdapter} derivative, which will keep every
     * loaded fragment in memory. If this becomes too memory intensive, it
     * may be best to switch to a
     * {@link android.support.v4.app.FragmentStatePagerAdapter}.
     */
    private SectionsPagerAdapter mSectionsPagerAdapter;

    /**
     * The {@link ViewPager} that will host the section contents.
     */
    private ViewPager mViewPager;

    public LocationManager locationManager;
    public String apiURL = "";
    public ScheduledThreadPoolExecutor mDialogDaemon;
    public int interval = 0;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main2);

        Toolbar toolbar = (Toolbar) findViewById(R.id.toolbar);
        setSupportActionBar(toolbar);
        // Create the adapter that will return a fragment for each of the three
        // primary sections of the activity.
        mSectionsPagerAdapter = new SectionsPagerAdapter(getSupportFragmentManager());

        // Set up the ViewPager with the sections adapter.
        mViewPager = (ViewPager) findViewById(R.id.container);
        mViewPager.setAdapter(mSectionsPagerAdapter);

        TabLayout tabLayout = (TabLayout) findViewById(R.id.tabs);
        tabLayout.setupWithViewPager(mViewPager);

        FloatingActionButton fab = (FloatingActionButton) findViewById(R.id.fab);
        fab.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                Snackbar.make(view, "Replace with your own action", Snackbar.LENGTH_LONG)
                        .setAction("Action", null).show();
            }
        });

        ActivityCompat.requestPermissions(this,new String[]{Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION, Manifest.permission.INTERNET}, 1001);

        locationManager = (LocationManager)getSystemService(Context.LOCATION_SERVICE);
    }


    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_main2, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        //noinspection SimplifiableIfStatement
        if (id == R.id.action_settings) {
            return true;
        }

        return super.onOptionsItemSelected(item);
    }

    /**
     * A placeholder fragment containing a simple view.
     */
    public static class PlaceholderFragment extends Fragment {
        /**
         * The fragment argument representing the section number for this
         * fragment.
         */
        private static final String ARG_SECTION_NUMBER = "section_number";

        public PlaceholderFragment() {
        }

        /**
         * Returns a new instance of this fragment for the given section
         * number.
         */
        public static PlaceholderFragment newInstance(int sectionNumber) {
            PlaceholderFragment fragment = new PlaceholderFragment();
            Bundle args = new Bundle();
            args.putInt(ARG_SECTION_NUMBER, sectionNumber);
            fragment.setArguments(args);
            return fragment;
        }

        @Override
        public View onCreateView(LayoutInflater inflater, ViewGroup container,
                                 Bundle savedInstanceState) {
            View rootView = null;

            switch (getArguments().getInt(ARG_SECTION_NUMBER)) {
                case 1: {
                    rootView = inflater.inflate(R.layout.fragment_main2, container, false);

                    EditText tbAPIUrl = (EditText)rootView.findViewById(R.id.tbAPIUrl);
                    ((Main2Activity)getActivity()).apiURL = tbAPIUrl.getText().toString();

                    break;
                }
                case 2: {
                    rootView = inflater.inflate(R.layout.fragment_main3, container, false);

                    EditText tbInterval = (EditText)rootView.findViewById(R.id.tbInterval);
                    ((Main2Activity)getActivity()).interval = Integer.parseInt(tbInterval.getText().toString());

                    Button button = (Button)rootView.findViewById(R.id.btnIntervalno);
                    button.setOnClickListener(new View.OnClickListener() {
                        public void onClick(View v) {
                            if (((Main2Activity)getActivity()).mDialogDaemon != null) {
                                ((Main2Activity)getActivity()).mDialogDaemon.shutdown();
                                ((Main2Activity)getActivity()).mDialogDaemon = null;
                            }
                            ((Main2Activity)getActivity()).mDialogDaemon = new ScheduledThreadPoolExecutor(1);
                            // This process will execute immediately, then execute every 3 seconds.
                            ((Main2Activity)getActivity()).mDialogDaemon.scheduleAtFixedRate(new Runnable() {
                                @Override
                                public void run() {
                                    ((Main2Activity)getActivity()).runOnUiThread(new Runnable() {
                                        @Override
                                        public void run() {
                                            try {
                                                LocationListener locationListener = new MyLocationListener((Main2Activity)getActivity(), ((Main2Activity)getActivity()).apiURL);
                                                ((Main2Activity)getActivity()).locationManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, 5000, 10, locationListener);
                                            }
                                            catch (SecurityException _ex) {
                                                Toast.makeText(getActivity(), _ex.getMessage(),
                                                        Toast.LENGTH_LONG).show();
                                            }
                                        }
                                    });
                                }
                            }, 0L, ((Main2Activity)getActivity()).interval, TimeUnit.SECONDS);
                        }
                    });

                    break;
                }
                case 3: {
                    rootView = inflater.inflate(R.layout.fragment_main4, container, false);

                    Button button = (Button)rootView.findViewById(R.id.btnEnkratno);
                    button.setOnClickListener(new View.OnClickListener() {
                        public void onClick(View v) {
                            try {
                                LocationListener locationListener = new MyLocationListener((Main2Activity)getActivity(), ((Main2Activity)getActivity()).apiURL);
                                ((Main2Activity)getActivity()).locationManager.requestLocationUpdates(LocationManager.GPS_PROVIDER, 5000, 10, locationListener);
                            }
                            catch (SecurityException _ex) {
                                Toast.makeText(getActivity(), _ex.getMessage(),
                                        Toast.LENGTH_LONG).show();
                            }
                        }
                    });

                    break;
                }
            }

            //TextView textView = (TextView) rootView.findViewById(R.id.section_label);
            //textView.setText(getString(R.string.section_format, getArguments().getInt(ARG_SECTION_NUMBER)));
            return rootView;
        }
    }

    /**
     * A {@link FragmentPagerAdapter} that returns a fragment corresponding to
     * one of the sections/tabs/pages.
     */
    public class SectionsPagerAdapter extends FragmentPagerAdapter {

        public SectionsPagerAdapter(FragmentManager fm) {
            super(fm);
        }

        @Override
        public Fragment getItem(int position) {
            // getItem is called to instantiate the fragment for the given page.
            // Return a PlaceholderFragment (defined as a static inner class below).
            return PlaceholderFragment.newInstance(position + 1);
        }

        @Override
        public int getCount() {
            // Show 3 total pages.
            return 3;
        }

        @Override
        public CharSequence getPageTitle(int position) {
            switch (position) {
                case 0:
                    return "Settings";
                case 1:
                    return "Interval";
                case 2:
                    return "One-time";
            }
            return null;
        }
    }
}
