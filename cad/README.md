# Geometry notes

The short elbow uses the specified 215 mm outside diameter, 150 mm height, 15 mm wall and 2.5 kg load. Its 30-degree circular geometry is a documented modelling approximation, not a scan of the reference video.

The STL coordinates and layout parameters use millimetres. Convert positions to metres when constructing RobotStudio world transforms. The vacuum tool TCP is 150 mm from the flange. Contact frames C1 and C2 are expressed in the same workpiece frame, so regrasp targets must use the matching contact transform.

The two Python generators create the geometry; the manifests record dimensions and checks. Keep the supplied runtime geometry unchanged when reproducing the recorded demonstration.
