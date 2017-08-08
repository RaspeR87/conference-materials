package com.kamensek.gasper.gkstepcounter;

import android.content.Context;
import android.content.SharedPreferences;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.location.LocationListener;
import android.location.LocationManager;
import android.support.design.widget.TabLayout;
import android.support.design.widget.FloatingActionButton;
import android.support.design.widget.Snackbar;
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

import java.util.Date;
import java.util.concurrent.ScheduledThreadPoolExecutor;
import java.util.concurrent.TimeUnit;

public class MainActivity extends AppCompatActivity implements SensorEventListener {

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

    public SensorManager sensorManager;
    public Sensor countSensor;
    boolean activityRunning;

    public SharedPreferences sharedPref;

    public String apiURL = "";
    public TextView count;
    public TextView count1;
    public TextView count2;

    private int zacStKorakov = -1;
    private Date datumZacetka;
    private int zacStKorakov1 = -1;
    private Date datumZacetka1;
    private int zacStKorakov2 = -1;

    private int szStKorakov = 0;
    private int szStKorakov1 = 0;
    private int szStKorakov2 = 0;
    private int tStKorakov;

    public ScheduledThreadPoolExecutor mDialogDaemon;
    public int interval = 0;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

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

        // preberi shranjeno začetno vrednost
        try {
            sharedPref = this.getPreferences(Context.MODE_PRIVATE);
            szStKorakov = sharedPref.getInt("szStKorakov", 0);
            szStKorakov1 = sharedPref.getInt("szStKorakov1", 0);
            szStKorakov2 = sharedPref.getInt("szStKorakov2", 0);
        }
        catch (Exception _ex) {
            String pom = "pom";
        }

        try {
            sensorManager = (SensorManager) getSystemService(Context.SENSOR_SERVICE);
        }
        catch (Exception _ex) {
            Toast.makeText(this, _ex.getMessage(), Toast.LENGTH_LONG).show();
        }

        datumZacetka = new Date();
        datumZacetka1 = new Date();
    }

    @Override
    protected void onResume() {
        super.onResume();
        try {
            activityRunning = true;

            countSensor = sensorManager.getDefaultSensor(Sensor.TYPE_STEP_COUNTER);

            if (countSensor != null) {
                sensorManager.registerListener(this, countSensor, SensorManager.SENSOR_DELAY_UI);
            } else {
                Toast.makeText(this, "Step counter senzor ni na voljo!", Toast.LENGTH_LONG).show();
            }
        }
        catch (Exception _ex) {
            Toast.makeText(this, _ex.getMessage(), Toast.LENGTH_LONG).show();
        }
    }

    @Override
    protected void onPause() {
        super.onPause();
        activityRunning = false;

        sensorManager.unregisterListener(this);
    }

    @Override
    public void onSensorChanged(SensorEvent event) {
        if (activityRunning) {
            try {
                if (zacStKorakov == -1) {
                    zacStKorakov = (int)event.values[0];
                    zacStKorakov1 = zacStKorakov;
                    zacStKorakov2 = zacStKorakov;
                }
                tStKorakov = (int)event.values[0];

                int nstKorakov = szStKorakov + (tStKorakov - zacStKorakov);
                count.setText(String.valueOf(nstKorakov));

                int nstKorakov1 = szStKorakov1 + (tStKorakov - zacStKorakov1);
                count1.setText(String.valueOf(nstKorakov1));

                int nstKorakov2 = szStKorakov2 + (tStKorakov - zacStKorakov2);
                count2.setText(String.valueOf(nstKorakov2));

                // shrani začetno vrednost
                try {
                    SharedPreferences.Editor editor = sharedPref.edit();
                    editor.putInt("szStKorakov", nstKorakov);
                    editor.putInt("szStKorakov1", nstKorakov1);
                    editor.putInt("szStKorakov2", nstKorakov2);
                    editor.commit();
                }
                catch (Exception _ex) {
                    String pom = "pom";
                }
            }
            catch (Exception _ex) {
                String pom = "pom";
            }
        }

    }

    @Override
    public void onAccuracyChanged(Sensor sensor, int accuracy) {
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_main, menu);
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
                    rootView = inflater.inflate(R.layout.fragment_main, container, false);

                    EditText tbAPIUrl = (EditText)rootView.findViewById(R.id.tbAPIUrl);
                    ((MainActivity)getActivity()).apiURL = tbAPIUrl.getText().toString();

                    break;
                }
                case 2: {
                    rootView = inflater.inflate(R.layout.fragment_main2, container, false);

                    ((MainActivity)getActivity()).count = (TextView)rootView.findViewById(R.id.tbStKorakov);
                    ((MainActivity)getActivity()).count1 = (TextView)rootView.findViewById(R.id.tbStKorakov1);
                    ((MainActivity)getActivity()).count2 = (TextView)rootView.findViewById(R.id.tbStKorakov2);

                    EditText tbInterval = (EditText)rootView.findViewById(R.id.tbInterval);
                    ((MainActivity)getActivity()).interval = Integer.parseInt(tbInterval.getText().toString());

                    Button btnReset1 = (Button)rootView.findViewById(R.id.btnReset1);
                    btnReset1.setOnClickListener(new View.OnClickListener() {
                        public void onClick(View v) {
                            ((MainActivity)getActivity()).datumZacetka1 = new Date();
                            ((MainActivity)getActivity()).zacStKorakov1 = ((MainActivity)getActivity()).tStKorakov;
                            ((MainActivity)getActivity()).szStKorakov1 = 0;
                            ((MainActivity)getActivity()).count1.setText("0");
                        }
                    });

                    Button btnReset2 = (Button)rootView.findViewById(R.id.btnReset2);
                    btnReset2.setOnClickListener(new View.OnClickListener() {
                        public void onClick(View v) {
                            ((MainActivity)getActivity()).zacStKorakov2 = ((MainActivity)getActivity()).tStKorakov;
                            ((MainActivity)getActivity()).szStKorakov2 = 0;
                            ((MainActivity)getActivity()).count2.setText("0");
                        }
                    });

                    Button button = (Button)rootView.findViewById(R.id.btnPricni);
                    button.setOnClickListener(new View.OnClickListener() {
                        public void onClick(View v) {
                            if (((MainActivity)getActivity()).mDialogDaemon != null) {
                                ((MainActivity)getActivity()).mDialogDaemon.shutdown();
                                ((MainActivity)getActivity()).mDialogDaemon = null;
                            }
                            ((MainActivity)getActivity()).mDialogDaemon = new ScheduledThreadPoolExecutor(1);
                            // This process will execute immediately, then execute every 3 seconds.
                            ((MainActivity)getActivity()).mDialogDaemon.scheduleAtFixedRate(new Runnable() {
                                @Override
                                public void run() {
                                    ((MainActivity)getActivity()).runOnUiThread(new Runnable() {
                                        @Override
                                        public void run() {
                                            try {
                                                new MyThread(Integer.parseInt(((MainActivity)getActivity()).count2.getText().toString()),
                                                        Integer.parseInt(((MainActivity)getActivity()).count1.getText().toString()),
                                                        Integer.parseInt(((MainActivity)getActivity()).count.getText().toString()),
                                                        ((MainActivity)getActivity()).datumZacetka1,
                                                        ((MainActivity)getActivity()).datumZacetka,
                                                        (MainActivity)getActivity(), ((MainActivity)getActivity()).apiURL).start();
                                            }
                                            catch (SecurityException _ex) {
                                                Toast.makeText(getActivity(), _ex.getMessage(),
                                                        Toast.LENGTH_LONG).show();
                                            }
                                        }
                                    });
                                }
                            }, 0L, ((MainActivity)getActivity()).interval, TimeUnit.SECONDS);
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
            return 2;
        }

        @Override
        public CharSequence getPageTitle(int position) {
            switch (position) {
                case 0:
                    return "Settings";
                case 1:
                    return "Counting";
            }
            return null;
        }
    }
}
