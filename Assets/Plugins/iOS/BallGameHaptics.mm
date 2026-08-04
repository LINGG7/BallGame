#import <CoreHaptics/CoreHaptics.h>

static CHHapticEngine *BallGameHapticEngine = nil;
static id<CHHapticAdvancedPatternPlayer> BallGameHapticPlayer = nil;
static BOOL BallGameHapticPlaying = NO;
static float BallGameHapticIntensity = 0.0f;
static float BallGameHapticSharpness = 0.0f;

static float BallGameClampIntensity(float intensity)
{
    return MAX(0.0f, MIN(1.0f, intensity));
}

static BOOL BallGameScheduleCurve(id<CHHapticAdvancedPatternPlayer> player,
                                  CHHapticDynamicParameterID parameterID,
                                  float startValue,
                                  float targetValue,
                                  NSTimeInterval duration) API_AVAILABLE(ios(13.0))
{
    CHHapticParameterCurveControlPoint *startPoint = [[CHHapticParameterCurveControlPoint alloc]
        initWithRelativeTime:0.0 value:startValue];
    CHHapticParameterCurveControlPoint *targetPoint = [[CHHapticParameterCurveControlPoint alloc]
        initWithRelativeTime:duration value:targetValue];
    CHHapticParameterCurve *curve = [[CHHapticParameterCurve alloc]
        initWithParameterID:parameterID
        controlPoints:@[startPoint, targetPoint]
        relativeTime:0.0];
    NSError *error = nil;
    [player scheduleParameterCurve:curve atTime:0.0 error:&error];
    return error == nil;
}

static BOOL BallGameEnsureEngine(void) API_AVAILABLE(ios(13.0))
{
    if (![CHHapticEngine.capabilitiesForHardware supportsHaptics])
    {
        return NO;
    }

    if (BallGameHapticEngine == nil)
    {
        NSError *error = nil;
        BallGameHapticEngine = [[CHHapticEngine alloc] initAndReturnError:&error];
        if (BallGameHapticEngine == nil || error != nil)
        {
            BallGameHapticEngine = nil;
            return NO;
        }

        BallGameHapticEngine.playsHapticsOnly = YES;
        BallGameHapticEngine.autoShutdownEnabled = NO;
        BallGameHapticEngine.stoppedHandler = ^(CHHapticEngineStoppedReason reason) {
            BallGameHapticPlayer = nil;
            BallGameHapticPlaying = NO;
        };
        BallGameHapticEngine.resetHandler = ^{
            BallGameHapticPlayer = nil;
            BallGameHapticPlaying = NO;
        };
    }

    NSError *startError = nil;
    [BallGameHapticEngine startAndReturnError:&startError];
    return startError == nil;
}

static BOOL BallGameStartPlayer(float intensity, float sharpnessValue) API_AVAILABLE(ios(13.0))
{
    if (!BallGameEnsureEngine())
    {
        return NO;
    }

    CHHapticEventParameter *eventIntensity = [[CHHapticEventParameter alloc]
        initWithParameterID:CHHapticEventParameterIDHapticIntensity value:1.0f];
    CHHapticEventParameter *sharpness = [[CHHapticEventParameter alloc]
        initWithParameterID:CHHapticEventParameterIDHapticSharpness value:0.0f];
    CHHapticEvent *event = [[CHHapticEvent alloc]
        initWithEventType:CHHapticEventTypeHapticContinuous
        parameters:@[eventIntensity, sharpness]
        relativeTime:0.0
        duration:30.0];

    NSError *error = nil;
    CHHapticPattern *pattern = [[CHHapticPattern alloc]
        initWithEvents:@[event] parameters:@[] error:&error];
    if (pattern == nil || error != nil)
    {
        return NO;
    }

    BallGameHapticPlayer = [BallGameHapticEngine createAdvancedPlayerWithPattern:pattern error:&error];
    if (BallGameHapticPlayer == nil || error != nil)
    {
        BallGameHapticPlayer = nil;
        return NO;
    }

    BallGameHapticPlayer.loopEnabled = YES;
    BallGameHapticPlayer.loopEnd = 30.0;
    BallGameHapticIntensity = BallGameClampIntensity(intensity);
    BallGameHapticSharpness = BallGameClampIntensity(sharpnessValue);

    CHHapticDynamicParameter *dynamicIntensity = [[CHHapticDynamicParameter alloc]
        initWithParameterID:CHHapticDynamicParameterIDHapticIntensityControl
        value:0.0f
        relativeTime:0.0];
    CHHapticDynamicParameter *dynamicSharpness = [[CHHapticDynamicParameter alloc]
        initWithParameterID:CHHapticDynamicParameterIDHapticSharpnessControl
        value:BallGameHapticSharpness
        relativeTime:0.0];
    [BallGameHapticPlayer sendParameters:@[dynamicIntensity, dynamicSharpness] atTime:0.0 error:&error];
    [BallGameHapticPlayer startAtTime:0.0 error:&error];
    BOOL attackScheduled = error == nil && BallGameScheduleCurve(
        BallGameHapticPlayer,
        CHHapticDynamicParameterIDHapticIntensityControl,
        0.0f,
        BallGameHapticIntensity,
        0.06);
    BallGameHapticPlaying = error == nil && attackScheduled;
    return BallGameHapticPlaying;
}

extern "C" void BallGameHapticsStartContinuous(float intensity, float sharpness)
{
    if (@available(iOS 13.0, *))
    {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (BallGameHapticPlayer != nil)
            {
                [BallGameHapticPlayer stopAtTime:0.0 error:NULL];
            }
            BallGameHapticPlayer = nil;
            BallGameHapticPlaying = NO;
            BallGameStartPlayer(intensity, sharpness);
        });
    }
}

extern "C" void BallGameHapticsUpdateContinuous(float intensity, float sharpness)
{
    if (@available(iOS 13.0, *))
    {
        dispatch_async(dispatch_get_main_queue(), ^{
            float clampedIntensity = BallGameClampIntensity(intensity);
            float clampedSharpness = BallGameClampIntensity(sharpness);
            if (!BallGameHapticPlaying || BallGameHapticPlayer == nil)
            {
                BallGameStartPlayer(clampedIntensity, clampedSharpness);
                return;
            }

            float previousIntensity = BallGameHapticIntensity;
            float previousSharpness = BallGameHapticSharpness;
            BallGameHapticIntensity = clampedIntensity;
            BallGameHapticSharpness = clampedSharpness;
            BOOL intensityScheduled = BallGameScheduleCurve(
                BallGameHapticPlayer,
                CHHapticDynamicParameterIDHapticIntensityControl,
                previousIntensity,
                clampedIntensity,
                0.06);
            BOOL sharpnessScheduled = BallGameScheduleCurve(
                BallGameHapticPlayer,
                CHHapticDynamicParameterIDHapticSharpnessControl,
                previousSharpness,
                clampedSharpness,
                0.06);
            if (!intensityScheduled || !sharpnessScheduled)
            {
                BallGameHapticPlaying = NO;
            }
        });
    }
}

extern "C" void BallGameHapticsStopContinuous(int immediate)
{
    if (@available(iOS 13.0, *))
    {
        dispatch_async(dispatch_get_main_queue(), ^{
            id<CHHapticAdvancedPatternPlayer> playerToStop = BallGameHapticPlayer;
            float intensityToFade = BallGameHapticIntensity;
            BallGameHapticPlayer = nil;
            BallGameHapticPlaying = NO;
            BallGameHapticIntensity = 0.0f;
            BallGameHapticSharpness = 0.0f;

            if (playerToStop == nil)
            {
                return;
            }

            if (immediate != 0)
            {
                [playerToStop stopAtTime:0.0 error:NULL];
                return;
            }

            BallGameScheduleCurve(
                playerToStop,
                CHHapticDynamicParameterIDHapticIntensityControl,
                intensityToFade,
                0.0f,
                0.06);
            dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.06 * NSEC_PER_SEC)),
                           dispatch_get_main_queue(), ^{
                [playerToStop stopAtTime:0.0 error:NULL];
            });
        });
    }
}
