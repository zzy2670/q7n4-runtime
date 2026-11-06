MODULE MA6212_OnePiece014
    ! One box of six actual parts; simulated camera data is read for each source.
    ! IRB1300 / OmniCore, TCP150. Fixture guides require vertical C2 lift.
    ! Camera offsets come from measured CAD pose, not the feed random-number table.
    ! First part is nominal; five following parts have independent random XY offsets.
    ! Actor capacity is six, with one empty-box acknowledgement before the batch.
    PERS bool single014ExecutionArmed:=FALSE;
    PERS num runId:=140301;
    PERS num cycleIndex:=1;
    PERS num slot:=0;
    PERS num dx:=0;
    PERS num dy:=0;
    PERS num stage:=0;
    PERS num runState:=0;
    PERS num completedCycles:=0;
    PERS num runAttempt:=0;
    PERS num lastError:=0;
    PERS num elapsed:=0;
    PERS bool motionSequenceComplete:=FALSE;
    PERS string runFile:="";
    PERS bool visionReady:=FALSE;
    PERS num visionSequence:=0;
    PERS num visionConsumedSequence:=0;
    PERS num visionDxMm:=0;
    PERS num visionDyMm:=0;
    PERS num visionVirtualTimeMs:=0;
    PERS string visionPartId:="";
    PERS string visionFrame:="";
    VAR num caseNumber;
    VAR robtarget measuredSourceContact;
    VAR robtarget measuredSourceLift;
    CONST speeddata vEmptyMoveJ:=[400,120,100,30];
    CONST speeddata vTravelCommission:=[400,120,100,30];
    CONST speeddata vApproachCommission:=[320,96,100,30];
    VAR clock cycleClock;
    VAR iodev cycleLog;
    VAR bool logOpen:=FALSE;
    VAR bool timingActive:=FALSE;
    PERS loaddata elbowLoadAtC1:=[2.5,[18.8352947,22.7884033,107.5],[0.706433772,-0.0308435638,-0.706433772,-0.0308435637],0.0135624012,0.0154981713,0.0231869125];
    PERS loaddata elbowLoadAtC2:=[2.5,[-18.8352947,1.30134567e-09,84.7115967],[0.521333805,0.477714416,-0.477714417,-0.521333805],0.0135624012,0.0154981713,0.0231869125];
    CONST robtarget sourceApproach:=[[602.9245075451499,-142.49999999999997,383.42727420088414],[0.5416752204197017,0.5416752204197021,-0.4545194776720435,0.4545194776720435],[0,-1,-1,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST robtarget sourceContact:=[[602.9245075451499,-242.5,383.4272742008841],[0.5416752204197017,0.5416752204197021,-0.4545194776720435,0.4545194776720435],[-1,-2,-1,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST robtarget sourceLift:=[[602.9245075451499,-242.5,483.4272742008841],[0.5416752204197017,0.5416752204197021,-0.4545194776720435,0.4545194776720435],[-1,-2,-1,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST robtarget boxApproach:=[[345.55530043978405,680,810.1261272331978],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST robtarget boxContact:=[[345.55530043978405,680,470.12612723319785],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST robtarget pBoxApproachSix{6}:=[
        [[345.55530043978405,680,810.1261272331978],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]],
        [[525.2557954783952,680,810.1261272331978],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]],
        [[704.9562905170061,680,810.1261272331978],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]],
        [[345.55530043978405,920,810.1261272331978],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]],
        [[525.2557954783952,920,810.1261272331978],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]],
        [[704.9562905170061,920,810.1261272331978],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]]];
    CONST robtarget pBoxContactSix{6}:=[
        [[345.55530043978405,680,470.12612723319785],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]],
        [[525.2557954783952,680,470.12612723319785],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]],
        [[704.9562905170061,680,470.12612723319785],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]],
        [[345.55530043978405,920,470.12612723319785],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]],
        [[525.2557954783952,920,470.12612723319785],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]],
        [[704.9562905170061,920,470.12612723319785],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]]];
    CONST robtarget pHome:=[[550,0,750],[0,1,0,0],[0,0,-3,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST robtarget pBoxVia:=[[450,200,810.1261272331978],[6.123233995736766E-17,1,1.232595164407831E-32,-3.015276387438861E-17],[0,0,-2,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST robtarget pFixturePlaceLift:=[[852.9245075451499,107.5,483.4272742008841],[0.5416752204197017,0.5416752204197021,-0.4545194776720435,0.4545194776720435],[0,-1,-1,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST robtarget pFixturePlace:=[[852.9245075451499,107.5,383.4272742008841],[0.5416752204197017,0.5416752204197021,-0.4545194776720435,0.4545194776720435],[0,-1,-1,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST robtarget pFixturePlaceApproach:=[[852.9245075451499,207.50000000000003,383.42727420088414],[0.5416752204197017,0.5416752204197021,-0.4545194776720435,0.4545194776720435],[0,-1,-1,0],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST robtarget pFixturePickApproach:=[[666.6229516856366,2.5411421082307575E-14,486.8080573302675],[5.015859645267624E-17,0.8191520442889917,-0,0.5735764363510463],[0,0,-3,1],[9E9,9E9,9E9,9E9,9E9,9E9]];
    CONST robtarget pFixturePick:=[[760.5922137642275,1.3164953090834047E-14,452.6060429977006],[5.015859645267624E-17,0.8191520442889917,-0,0.5735764363510463],[-1,0,-3,1],[9E9,9E9,9E9,9E9,9E9,9E9]];

    PROC main()
        IF NOT single014ExecutionArmed THEN
            TPWrite "OnePiece014 is not released for execution.";
            RETURN;
        ENDIF
        ! Consume one-shot permission before any possible motion.
        single014ExecutionArmed:=FALSE;
        IF runAttempt<>0 OR StrLen(runFile)=0 THEN
            TPWrite "Fresh module and unique HOME CSV runFile required.";
            Stop;
            RETURN;
        ENDIF
        runAttempt:=1;
        lastError:=0;
        completedCycles:=0;
        motionSequenceComplete:=FALSE;
        timingActive:=FALSE;
        elapsed:=0;
        stage:=0;
        runState:=1;
        Open "HOME:"+runFile,cycleLog\Append;
        logOpen:=TRUE;
        Write cycleLog,"run,cycle,dx_mm,dy_mm,slot,cycle_s,state,stage,err,"\NoNewLine;
        Write cycleLog,"vision_part_id,vision_seq,vision_virtual_ms";
        ConfJ \On;
        ConfL \On;
        SetDO doVacuum,0;
        WaitDI diVacuumOK,0\MaxTime:=2;
        SetDO doFeedRequest,0;
        WaitDI diFeedReady,0\MaxTime:=5;
        SetDO doBoxReset,0;
        WaitDI diBoxReady,0\MaxTime:=5;
        MoveJ pHome,vEmptyMoveJ,fine,tVacuum\WObj:=wobj0;
        ResetBox;
        FOR caseNumber FROM 1 TO 6 DO
            cycleIndex:=caseNumber;
            slot:=cycleIndex-1;
            visionReady:=FALSE;
            stage:=8;
            SetDO doFeedRequest,1;
            WaitDI diFeedReady,1\MaxTime:=5;
            WaitUntil visionReady AND visionSequence=cycleIndex\MaxTime:=5;
            IF visionSequence<=visionConsumedSequence OR visionFrame<>"station_world" OR
               StrLen(visionPartId)=0 OR Abs(visionDxMm)>5 OR Abs(visionDyMm)>5 THEN
                lastError:=-100;
                runState:=-1;
                WriteCycle "INVALID_VISION";
                Close cycleLog;
                logOpen:=FALSE;
                Stop;
                RETURN;
            ENDIF
            ! These measured offsets are the only source-position correction inputs.
            dx:=visionDxMm;
            dy:=visionDyMm;
            visionConsumedSequence:=visionSequence;
            visionReady:=FALSE;
            ClkReset cycleClock;
            ClkStart cycleClock;
            timingActive:=TRUE;
            TransferViaFixture;
            MoveJ pHome,vEmptyMoveJ,fine,tVacuum\WObj:=wobj0;
            ClkStop cycleClock;
            elapsed:=ClkRead(cycleClock);
            timingActive:=FALSE;
            WriteCycle "MOTION_SEQUENCE_RETURNED";
            SetDO doFeedRequest,0;
            WaitDI diFeedReady,0\MaxTime:=5;
            completedCycles:=cycleIndex;
        ENDFOR
        motionSequenceComplete:=TRUE;
        stage:=0;
        runState:=2;
        Close cycleLog;
        logOpen:=FALSE;
        Stop;
    ERROR
        lastError:=ERRNO;
        runState:=-1;
        motionSequenceComplete:=FALSE;
        IF timingActive THEN
            ClkStop cycleClock;
            elapsed:=ClkRead(cycleClock);
            timingActive:=FALSE;
        ENDIF
        IF logOpen THEN
            WriteCycle "ERROR";
            Close cycleLog;
            logOpen:=FALSE;
        ENDIF
        ! Preserve current vacuum/output state for inspection; no blind release/retry.
        RAISE;
    ENDPROC

    PROC TransferViaFixture()
        ! Current identity-wobj0/world layout: source branch uses measured Y.
        ! Root must still validate both branches with actual CalcJointT and paths.
        measuredSourceContact:=Offs(sourceContact,dx,dy,0);
        measuredSourceLift:=Offs(sourceLift,dx,dy,0);
        IF dy>0 THEN
            measuredSourceContact.robconf:=[0,-1,-1,0];
            measuredSourceLift.robconf:=[0,-1,-1,0];
        ELSE
            measuredSourceContact.robconf:=[-1,-2,-1,0];
            measuredSourceLift.robconf:=[-1,-2,-1,0];
        ENDIF
        stage:=10;
        MoveJ Offs(sourceApproach,dx,dy,0),vEmptyMoveJ,fine,tVacuum\WObj:=wobj0;
        MoveL measuredSourceContact,vApproachCommission,fine,tVacuum\WObj:=wobj0;
        WaitTime \InPos,0.25;
        SetDO doVacuum,1;
        WaitDI diVacuumOK,1\MaxTime:=2;
        GripLoad elbowLoadAtC1;
        MoveL measuredSourceLift,vApproachCommission,fine,tVacuum\WObj:=wobj0;
        stage:=20;
        MoveJ pFixturePlaceLift,vTravelCommission,fine,tVacuum\WObj:=wobj0;
        MoveL pFixturePlace,vApproachCommission,fine,tVacuum\WObj:=wobj0;
        WaitTime \InPos,0.25;
        SetDO doVacuum,0;
        WaitDI diVacuumOK,0\MaxTime:=2;
        GripLoad load0;
        MoveL pFixturePlaceApproach,vApproachCommission,fine,tVacuum\WObj:=wobj0;
        WaitTime 0.5;
        stage:=30;
        ! Empty-tool transit via existing Ready; actual MoveJ collision check still required.
        MoveJ pHome,vEmptyMoveJ,fine,tVacuum\WObj:=wobj0;
        MoveJ pFixturePickApproach,vEmptyMoveJ,fine,tVacuum\WObj:=wobj0;
        MoveL pFixturePick,vApproachCommission,fine,tVacuum\WObj:=wobj0;
        WaitTime \InPos,0.25;
        SetDO doVacuum,1;
        WaitDI diVacuumOK,1\MaxTime:=2;
        GripLoad elbowLoadAtC2;
        ! Offs is world+Z ONLY after Root verifies wobj0 axes equal station world.
        ! Clear the7mm guides by10mm, fine; next MoveL rises another24.202mm.
        MoveL Offs(pFixturePick,0,0,10),vApproachCommission,fine,tVacuum\WObj:=wobj0;
        MoveL pFixturePickApproach,vApproachCommission,fine,tVacuum\WObj:=wobj0;
        stage:=40;
        MoveJ pBoxVia,vTravelCommission,fine,tVacuum\WObj:=wobj0;
        MoveL pBoxApproachSix{slot+1},vApproachCommission,fine,tVacuum\WObj:=wobj0;
        MoveL pBoxContactSix{slot+1},vApproachCommission,fine,tVacuum\WObj:=wobj0;
        WaitTime \InPos,0.25;
        SetDO doVacuum,0;
        WaitDI diVacuumOK,0\MaxTime:=2;
        GripLoad load0;
        MoveL pBoxApproachSix{slot+1},vApproachCommission,fine,tVacuum\WObj:=wobj0;
        stage:=50;
    ENDPROC

    PROC ResetBox()
        stage:=5;
        SetDO doBoxReset,1;
        WaitDI diBoxReady,1\MaxTime:=5;
        SetDO doBoxReset,0;
        WaitDI diBoxReady,0\MaxTime:=5;
    ENDPROC

    PROC WriteCycle(string stateText)
        ! Each chunk stays within the RAPID string limit; together they form 12 CSV columns.
        Write cycleLog,NumToStr(runId,0)+","+NumToStr(cycleIndex,0)+","+NumToStr(dx,3)+","+
            NumToStr(dy,3)+","+NumToStr(slot,0)+","\NoNewLine;
        Write cycleLog,NumToStr(elapsed,4)+","+stateText+","+NumToStr(stage,0)+","+
            NumToStr(lastError,0)+","\NoNewLine;
        Write cycleLog,visionPartId+","\NoNewLine;
        Write cycleLog,NumToStr(visionConsumedSequence,0)+","+NumToStr(visionVirtualTimeMs,1);
    ENDPROC
ENDMODULE
