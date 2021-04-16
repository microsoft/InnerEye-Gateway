# Editing Sequence Diagram

The [sequence diagram](sequence.wsd) is stored in the [PlantUML](https://plantuml.com/) text format.

## PlantUML

To build a png image file from the wsd file, follow the PlantUML [Getting Started](https://plantuml.com/starting):

1. Install [Java](https://www.java.com/en/download/).

1. Download [plantuml.jar](http://sourceforge.net/projects/plantuml/files/plantuml.jar/download)

### Preview

To show a live preview of the image whilst editting the file:

1. Run the command:

```cmd
java -jar plantuml.jar
```

2. Change the file extensions text box to show "wsd"

1. Select "sequence.png [sequence.wsd]" in the file list.

1. A preview of the image will be shown in a new window and will update automatically as the text file is editted.

### Export

To create an image from the file run the command:

```cmd
java -jar plantuml.jar sequence.wsd
```

## Visual Studio Code

There are PlantUML extension available for [Visual Studio Code](https://code.visualstudio.com/), for example [PlantUML](https://marketplace.visualstudio.com/items?itemName=jebbs.plantuml) which offer previews of the final image and image export functions.
