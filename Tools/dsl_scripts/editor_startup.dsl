script(main)
{
	($r, $argv) = has_cmd_arg("-i");
	if ($r) {
		if ($argv != "do_pp" && display_dialog("确认","是否要跳过资源后处理？","是","否")) {
			skip_asset_postprocessor(true);
		};
	}
	else {
		skip_asset_postprocessor(true);
	};
};