@echo off
echo Starting Folder Replicator Test...
cd ..
dotnet run -- --source "test/source" --destination "test/destination" --logfile "test/test.log" --verbose --once
echo Test completed. Check test/destination for results.
pause
