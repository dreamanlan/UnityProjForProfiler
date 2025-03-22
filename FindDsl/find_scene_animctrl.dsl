input("*.controller")
{
    int("maxKeyFrameCount", 500);
    stringlist("filter", "");
    stringlist("notfilter", "");
	float("pathwidth",240){range(20,4096);};
    feature("source", "sceneassets");
    feature("menu", "2.Current Scene Resources/Animation Controller");
    feature("description", "just so so");
}
filter
{
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter)){
        $v0 = loadasset(assetpath);
        $v1 = collectanimatorcontrollerinfo($v0);
        //unloadasset($v0);
        order = $v1.maxKeyFrameCount;
        if($v1.maxKeyFrameCount >= maxKeyFrameCount){
            $v2 = $v1.clips.orderbydesc($$.maxKeyFrameCount).top(4);
            $v3 = newstringbuilder();
            appendlineformat($v3, "clip name:{0}, total max keyframe count:{1}, layer count:{2}, state count:{3}, sub state machine count:{4}",
                $v1.maxKeyFrameClipName, $v1.maxKeyFrameCount, $v1.layerCount, $v1.stateCount, $v1.subStateMachineCount
                );
            looplist($v2){
                appendlineformat($v3, "clip name:{0}, max keyframe count:{1}", $$.clipName, $$.maxKeyFrameCount);
            };
            info = stringbuildertostring($v3);
            1;
        }else{
            0;
        };
    }else{
        0;
    };
};