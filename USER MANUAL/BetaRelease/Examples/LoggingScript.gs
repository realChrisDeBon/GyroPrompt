NEW INT Counter 0
:START
PRINT Begin counting.
INQUIRY 1 Username Please enter a name
FILESYSTEM WRITE Logfile.txt Entry# Time      Description{L} 
:PRINTLOG
PRINT The time is {T}.
FILESYSTEM WRITE Logfile.txt {Counter}: {T} Entry made during {Username} session.{L} 
ADD1 Counter
IF Counter < 16 THEN EXEC PRINTLOG
:END
TERMINATE