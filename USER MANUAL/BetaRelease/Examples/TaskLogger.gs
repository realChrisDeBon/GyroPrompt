TASK NEW Logger 1
TASK ADD Logger FILESYSTEM WRITE Log.txt Logging time: {T}{L}
NEW INT YesNo 0
POPUP MESSAGE Makes a log every second!
:START
INQUIRY 0 _YesNo Start logger?
IF YesNo = 1 THEN TASK START Logger
IF YesNo = 0 THEN EXEC START
:STOP
INQUIRY 0 _YesNo Stop logger now?
IF YesNo = 1 THEN TERMINATE
IF YesNo = 0 THEN EXEC STOP