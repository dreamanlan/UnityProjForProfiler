input("*.anim")
{
    int("maxKeyFrameCount", 500);
    stringlist("filter", "");
    stringlist("notfilter", "");
	float("pathwidth",240){range(20,4096);};
    feature("source", "sceneassets");
    feature("menu", "2.Current Scene Resources/Animation Clip");
    feature("description", "just so so");
}
filter
{
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter)){
        $v0 = getanimationclipinfo();
        order = $v0.maxKeyFrameCount;
        if($v0.maxKeyFrameCount>=maxKeyFrameCount){
            info = format($v1, "clip_name:{0},max_keyframe_count:{1},curve:{2}",
                $v0.clipName, $v0.maxKeyFrameCount, $v0.maxKeyFrameCurveName
                );
            1;
        };
    }else{
        0;
    };
};