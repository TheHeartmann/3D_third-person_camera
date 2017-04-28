# 3D, third-person camera

Working on building a third person camera for use in 3D Unity projects. The aim of the project is to create a package that can easily be dropped into a project and "just work". Ultimately, the goal would be to release this on the asset store.

For the latest exported version, see the Packages directory.

## Requirements

At the moment the camera requires the object to focus on to have the tag "Player". The camera also uses "zoom in" and "zoom out" buttons, so if these are not defined in the input pane of the project settings, it will throw an error.

Also required is a tag for unobtrusive objects, i.e. objects that are allowed to obstruct the camera's view of the player. By default this tag is named "Unobtrusive", but this can be changed in the inspector.

## References

This project was originally based on [a tutorial by Gus the Shark](https://veilwalker.wordpress.com/2016/06/29/the-secrets-to-building-a-smooth-3rd-person-camera-rig/#more-65), but has since been expanded to include other elements as well, drawing heavily on points mentioned in [50 Game Camera Mistakes](www.youtube.com/watch?v=C7307qRmlMI) by John Nesky.