PRINT Now running GyroChain script...
TITLE GyroChain Script
NEW STR Input Null
NEW INT Boolean 0
NEW INT AskBlock 0
BLOCKCHAIN ERECT GyroChain
:START
POPUP INQUIRY 0 AskBlock Create new block?
IF AskBlock = 0 THEN EXEC ResponseNo
IF AskBlock = 1 THEN EXEC ResponseYes
:ResponseNo
BLOCKCHAIN VIEW BLOCKS GyroChain
INQUIRY 0 _Boolean End GyroChain Script?
IF Boolean = 1 THEN EXEC END
IF Boolean = 0 THEN EXEC START
:ResponseYes
INQUIRY 1 _Input New data for new block
BLOCKCHAIN NEWBLOCK GyroChain {Input}
BLOCKCHAIN VIEW BLOCKS GyroChain
EXEC START
:END
POPUP MESSAGE Exporting GyroChain.
BLOCKCHAIN EXPORT GyroChain