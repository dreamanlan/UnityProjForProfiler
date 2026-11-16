input("*.anim")
{
    int("maxKeyFrameCount", 500);
    stringlist("filter", "");
    stringlist("notfilter", "");
	float("pathwidth",240){range(20,4096);};
    feature("source", "project");
    feature("menu", "1.Project Resources/Animation Clip");
    feature("description", "just so so");
}
filter
{
    if(string_contains(asset_path, filter) && string_not_contains(asset_path, notfilter)){
        $v0 = get_animation_clip_info();
        order = $v0.maxKeyFrameCount;
        if($v0.maxKeyFrameCount>=maxKeyFrameCount){
            info = format("clip_name:{0},max_keyframe_count:{1},curve:{2}",
                $v0.clipName, $v0.maxKeyFrameCount, $v0.maxKeyFrameCurveName
                );
            1;
        };
    }else{
        0;
    };
};