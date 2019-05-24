# MagCalTool
Magnetometer calibration GUI.  Uses Octave to run MATLAB script on magnetometer point cloud

This tool acquires 3D raw magnetometer data via the serial port, displays the raw result in a 3D window, and then generates a 3x3 compensation matrix and center offset.  The matrix values and center offset can then be used in your program to compensate the target device.

Serial data is expected to be comma, space, or tab delimited.  The program accepts and displays 1,2, or 3D data, but of course calibration matrix generation is only valid for 3D data that sufficiently covers the entire sphere of measurements (sufficient here is usually 500-1000 points that generally covever the sphere.  More points ==> better calibration.

You must have Octave installed for the calibration matrix generation step to work.  You can still record and visualize 3D magnetometer data, but the program calls Octave to run the MATLAB calibration routine

19 July 2017 Note:  I have had one report that the viewer fails to properly view a set of 3D points when the host PC is set for a non-English region.  If this happens to you, please reset your region to English.

23 May 2019: Fixed some minor GUI bugs.

It was possible to click on the 'Compute' button even with an empty data list - fixed
The 'Save Vals To Text File...' button launched an 'Open File' dialog instead of a 'Save File' dialog - fixed
