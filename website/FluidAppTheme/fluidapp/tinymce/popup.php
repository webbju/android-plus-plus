<?php

// loads the shortcodes class, wordpress is loaded with it
require_once( 'shortcodes.class.php' );

// get popup type
$popup = trim( $_GET['popup'] );
$shortcode = new tz_shortcodes( $popup );

?>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head></head>
<body>
<div id="tz-popup">

	<div id="tz-shortcode-wrap">
		
		<div id="tz-sc-form-wrap">
		
			<div id="tz-sc-form-head">
			
				<?php echo $shortcode->popup_title; ?>
			
			</div>
			<!-- /#tz-sc-form-head -->
			
			<form method="post" id="tz-sc-form">
			
				<table id="tz-sc-form-table">
				
					<?php echo $shortcode->output; ?>
					
					<tbody>
						<tr class="form-row">
							<?php if( ! $shortcode->has_child ) : ?><td class="label">&nbsp;</td><?php endif; ?>
							<td class="field"><a href="#" class="button-primary tz-insert">Insert Shortcode</a></td>							
						</tr>
					</tbody>
				
				</table>
				<!-- /#tz-sc-form-table -->
				
			</form>
			<!-- /#tz-sc-form -->
		
		</div>
		<!-- /#tz-sc-form-wrap -->
		
		<div id="tz-sc-preview-wrap">
		
			<div id="tz-sc-preview-head">
		
				Shortcode Preview
					
			</div>
			<!-- /#tz-sc-preview-head -->
			
			<?php if( $shortcode->no_preview ) : ?>
			<div id="tz-sc-nopreview">Shortcode has no preview</div>		
			<?php else : ?>			
			<iframe src="<?php echo TZ_TINYMCE_URI; ?>/preview.php?sc=" width="249" frameborder="0" id="tz-sc-preview"></iframe>
			<?php endif; ?>
			
		</div>
		<!-- /#tz-sc-preview-wrap -->
		
		<div class="clear"></div>
		
	</div>
	<!-- /#tz-shortcode-wrap -->

</div>
<!-- /#tz-popup -->

</body>
</html>