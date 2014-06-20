<?php
/*
Template Name: Contact
*/
?>
<?php 
if(isset($_POST['submitted'])) {
	
	$to = get_option('t2t_contact_form_email');
	if (!isset($to) || ($to == '') ){
		$to = get_option('admin_email');
	}
	
	// Extract form contents
	$full_name = $_POST['full_name'];
	$email = $_POST['email'];
	$website = $_POST['website'];
	$subject = $_POST['subject'];
	$message = $_POST['message'];
		
	// Validate email address
	function valid_email($str) {
		return ( ! preg_match("/^([a-z0-9\+_\-]+)(\.[a-z0-9\+_\-]+)*@([a-z0-9\-]+\.)+[a-z]{2,6}$/ix", $str)) ? FALSE : TRUE;
	}
	
	// Return errors if present
	$errors = "";
	
	if($full_name =='') { $errors .= "full_name,"; }
	if(valid_email($email)==FALSE) { $errors .= "email,"; }
	if($message =='') { $errors .= "message,"; }

	// Send email
	if($errors =='') {

		$headers =  'From: FluidApp <no-reply@fluidapp.com>'. "\r\n" .
					'Reply-To: '.$email.'' . "\r\n" .
					'X-Mailer: PHP/' . phpversion();
		$email_subject = "Website Contact Form: $email";
		$message="Name: $full_name \n\nEmail: $email \n\nWebsite: $website \n\nSubject: $subject \n\nMessage:\n\n$message";
	
		mail($to, $email_subject, $message, $headers);
		echo "true";
	
	} else {
		echo $errors;
	}
		
} else {
 ?>
<?php get_header(); ?>
<div class="page">

	<?php while (have_posts()) : the_post(); ?>

		<h1>
			<?php 
				global $post;
				the_title();
			?>
		</h1>

		<?php the_content(); ?>

	<?php endwhile; ?>
	
	<div id="contact_form">
	
		<div class="validation">
			<p>Oops! Please correct the highlighted fields...</p>
		</div>

		<div class="success">
			<p>Thanks! I'll get back to you shortly.</p>
		</div>

		<form action="<?php the_permalink(); ?>" method="post">
			<div class="row">
				<p class="left">
					<label for="full_name"><?php _e('Name*', 'framework') ?></label>
					<input type="text" name="full_name" id="full_name" value="" />
				</p>
				<p class="right">
					<label for="email"><?php _e('Email*', 'framework') ?></label>
					<input type="text" name="email" id="email" value="" />
				</p>
			</div>
	
			<div class="row">
				<p class="left">
					<label for="website"><?php _e('Website', 'framework') ?></label>
					<input type="text" name="website" id="website" value="" />
				</p>
				<p class="right">
					<label for="subject"><?php _e('Subject', 'framework') ?></label>
					<input type="text" name="subject" id="subject" value="" />
				</p>
			</div>
	
			<p>
				<label for="message" class="textarea"><?php _e('Message', 'framework') ?></label>
				<textarea class="text" name="message" id="message"></textarea>
			</p>
			<input type="hidden" name="submitted" id="submitted" value="true" />
			<input type="submit" class="button white" value="<?php _e('Send &#x2192;', 'framework') ?>" />
		</form>
	
	</div>
</div>
<?php get_footer(); ?>
<?php }  ?>