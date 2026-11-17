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
    if(string_contains(asset_path, filter) && string_not_contains(asset_path, notfilter)){
        $v0 = load_asset(asset_path);
        $v1 = collect_animator_controller_info($v0);
        order = $v1.maxKeyFrameCount;
        if($v1.maxKeyFrameCount >= maxKeyFrameCount){
            $v2 = $v1.clips.orderbydesc($$.maxKeyFrameCount).top(4);
            $v3 = new_string_builder();
            append_format_line($v3, "clip name:{0}, total max keyframe count:{1}, layer count:{2}, state count:{3}, sub state machine count:{4}",
                $v1.maxKeyFrameClipName, $v1.maxKeyFrameCount, $v1.layerCount, $v1.stateCount, $v1.subStateMachineCount
                );
            loop_list($v2){
                append_format_line($v3, "clip name:{0}, max keyframe count:{1}", $$.clipName, $$.maxKeyFrameCount);
            };
            info = stringbuilder_tostring($v3);
            $r = 1;
        }else{
            $r = 0;
        };
        unload_asset($v0);
        $r;
    }else{
        0;
    };
};