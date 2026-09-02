echo "Deleting old .zip if it exists..."
rm ConsistencyTrackerMod.zip
echo "Creating new archive..."
zip -r ConsistencyTrackerMod.zip everest.yaml Assets Dialog bin/ConsistencyTracker.dll bin/ConsistencyTracker.pdb
echo "Done"
