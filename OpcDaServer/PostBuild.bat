if %1[==[ goto default

if exist %2DANSrv.exe copy %2DANSrv.exe %2%1
if not exist %2DANSrv.exe copy %2..\..\DANSrv.exe %2%1
if exist %2DANSrv.exe.config copy %2DANSrv.exe.config %2%1
if exist %2DANSrv.Items.xml copy %2DANSrv.Items.xml %2%1
if exist %2RegServer.exe copy %2RegServer.exe %2%1
if not exist %2RegServer.exe copy %2..\..\RegServer.exe %2%1
if exist %2UnregServer.exe copy %2UnregServer.exe %2%1
if not exist %2UnregServer.exe copy %2..\..\UnregServer.exe %2%1
%2%1DANSrv.exe /RegServer
goto done

:default
if exist DANSrv.exe copy DANSrv.exe bin
if not exist DANSrv.exe copy ..\..\DANSrv.exe bin
if exist DANSrv.exe.config copy DANSrv.exe.config bin
if exist DANSrv.Items.xml copy DANSrv.Items.xml bin
if exist RegServer.exe copy RegServer.exe bin
if not exist RegServer.exe copy ..\..\RegServer.exe bin
if exist UnregServer.exe copy UnregServer.exe bin
if not exist UnregServer.exe copy ..\..\UnregServer.exe bin
bin\DANSrv.exe /RegServer

:done