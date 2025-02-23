using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeechPlayground.Services
{
    public static class ServiceConstants
    {
        public const string VerseDetectionPrompt = @"
Using this JSON schema/format for context, and the king james version of the Christian bible, only return this. 

verses schema: { ""book"": ""bookname"", ""verses"": [array of int], ""chapter"": int, ""confidence"": decimal, ""relevance"": decimal, ""paraphrase"": string }

{ ""MainVerses"": [verses objects], ""ReferenceVerses"": [verses objects] }

Return an array of this schema, with the first object in the MainVerses array being the book/chapter/verses being the primary topic of discussion and being currently read or the most recently read by the pastor. If he switches books/chapters then it should switch the primary verse returned in MainVerses.
ReferenceVerses is another array of verse objects for verses that were mentioned or quoted in passing by the pastor in the transcript but not necessarily read directly from the bible. 
If the pastor specifically mentions a book and verse, or if the pastor directly or indirectly quotes or even paraphrases a verse in the bible, include those in the ReferenceVerses. Check all spoken words for references.
Confidence in the json object is your confidence on a scale of 0-1 decimal of accuracy. 
Relevance is the relevance in regards to the entire topic of discussion and the main points of discussion on a scale of 0-1 decimal. 
Verses is the range of verses being referenced in the same book/chapter.
Paraphrase is the spoken words of the pastor directly from the transcript.
Only answer in this json format without extra language or formatting using the king james version of the bible.

If you know the book, but don't know the verse or are uncertain of any main verses, do not return any ints in the verses array. Only if certain the verse numbers.

The transcript may be inaccurate due to live translation. Timestamps are included with the spoken words.

Make sure the verses you return are valid verses that exist in the book and chapter that you return from the king james bible.

If ReferenceVerses are in the same book and chapter as one of the MainVerses, include them in the MainVerses grouped by book/chapter. Do not include MainVerse book/chapter in ReferenceVerses separately.

Do not change your MainVerses frequently, they should stick since this is a lesson given by the pastor. Frequently changing MainVerses causes frequently changing display for users watching hte live stream of the pastors message.

MainVerses are the main topic of conversation. When a pastor quotes or paraphrases indirectly, those verses should be returned as ReferenceVerses and not MainVerses.

If you detect a MainVerse, and know the chapter but not the specific verse he is going to read, make the verses array of that MainVerse object an empty array. Do not guess the verse number.

Only include verses directly read in the MainVerses.

Remember your previous answers so you aren't constantly switching the main verse back and forth or the verses within the mainverse object. I am using this as an overlay to the pastor's livestream. Be consistent.\r\n\r\n";
    }
}
