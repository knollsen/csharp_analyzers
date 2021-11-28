## Instructions

The project using this analyzer needs to include a namingConfig.json file in its root. The json needs to have the following structure:

```
{
  "DisallowedTermsInClassNames": [ "Class" ], // terms that cannot appear in class names
  "DisallowedSuffixesInMembersType":
  [
    {
      "ClassSuffix": "Factory", // suffix of the classes that this rule applies to
      "DisallowedMemberSuffixes": [ "Repository" ] // suffixes of members that are not allowed to appear in this class
    }
  ] 
}
```
