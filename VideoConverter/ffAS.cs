using FFmpeg.AutoGen;
using System;
using System.IO;

namespace ConsoleApp1
{
    //adapted using code from https://stackoverflow.com/questions/32051847/c-ffmpeg-distorted-sound-when-converting-audio?rq=1
    public unsafe class Program
    {
        public static AVStream* in_audioStream { get; private set; }

        static unsafe void die(string str)
        {
            throw new Exception(str);
        }

        private static unsafe AVStream* add_audio_stream(AVFormatContext* oc, AVCodecID codec_id, int sample_rate = 44100)
        {
            AVCodecContext* c;
            AVCodec* encoder = ffmpeg.avcodec_find_encoder(codec_id);
            AVStream* st = ffmpeg.avformat_new_stream(oc, encoder);

            if (st == null)
            {
                die("av_new_stream");
            }

            c = st->codec;
            c->codec_id = codec_id;
            c->codec_type = AVMediaType.AVMEDIA_TYPE_AUDIO;

            /* put sample parameters */
            c->bit_rate = 64000;
            c->sample_rate = sample_rate;
            c->channels = 2;
            c->sample_fmt = encoder->sample_fmts[0];
            c->channel_layout = ffmpeg.AV_CH_LAYOUT_STEREO;

            // some formats want stream headers to be separate
            if ((oc->oformat->flags & ffmpeg.AVFMT_GLOBALHEADER) != 0)
            {
                c->flags |= ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
            }

            return st;
        }

        private static unsafe void open_audio(AVFormatContext* oc, AVStream* st)
        {
            AVCodecContext* c = st->codec;
            AVCodec* codec;

            /* find the audio encoder */
            codec = ffmpeg.avcodec_find_encoder(c->codec_id);
            if (codec == null)
            {
                die("avcodec_find_encoder");
            }

            /* open it */
            AVDictionary* dict = null;
            ffmpeg.av_dict_set(&dict, "strict", "+experimental", 0);
            int res = ffmpeg.avcodec_open2(c, codec, &dict);
            if (res < 0)
            {
                die("avcodec_open");
            }
        }
        public static int DecodeNext(AVCodecContext* avctx, AVFrame* frame, ref int got_frame_ptr, AVPacket* avpkt)
        {
            int ret = 0;
            got_frame_ptr = 0;
            if ((ret = ffmpeg.avcodec_receive_frame(avctx, frame)) == 0)
            {
                //0 on success, otherwise negative error code
                got_frame_ptr = 1;
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                //AVERROR(EAGAIN): input is not accepted in the current state - user must read output with avcodec_receive_packet()
                //(once all output is read, the packet should be resent, and the call will not fail with EAGAIN)
                ret = Decode(avctx, frame, ref got_frame_ptr, avpkt);
            }
            else if (ret == ffmpeg.AVERROR_EOF)
            {
                die("AVERROR_EOF: the encoder has been flushed, and no new frames can be sent to it");
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.EINVAL))
            {
                die("AVERROR(EINVAL): codec not opened, refcounted_frames not set, it is a decoder, or requires flush");
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.ENOMEM))
            {
                die("Failed to add packet to internal queue, or similar other errors: legitimate decoding errors");
            }
            else
            {
                die("unknown");
            }
            return ret;
        }
        public static int Decode(AVCodecContext* avctx, AVFrame* frame, ref int got_frame_ptr, AVPacket* avpkt)
        {
            int ret = 0;
            got_frame_ptr = 0;
            if ((ret = ffmpeg.avcodec_send_packet(avctx, avpkt)) == 0)
            {
                //0 on success, otherwise negative error code
                return DecodeNext(avctx, frame, ref got_frame_ptr, avpkt);
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                die("input is not accepted in the current state - user must read output with avcodec_receive_frame()(once all output is read, the packet should be resent, and the call will not fail with EAGAIN");
            }
            else if (ret == ffmpeg.AVERROR_EOF)
            {
                die("AVERROR_EOF: the decoder has been flushed, and no new packets can be sent to it (also returned if more than 1 flush packet is sent");
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.EINVAL))
            {
                die("codec not opened, it is an encoder, or requires flush");
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.ENOMEM))
            {
                die("Failed to add packet to internal queue, or similar other errors: legitimate decoding errors");
            }
            else
            {
                die("unknown");
            }
            return ret;//ffmpeg.avcodec_decode_audio4(fileCodecContext, audioFrameDecoded, &frameFinished, &inPacket);
        }
        public static int DecodeFlush(AVCodecContext* avctx, AVPacket* avpkt)
        {
            avpkt->data = null;
            avpkt->size = 0;
            return ffmpeg.avcodec_send_packet(avctx, avpkt);
        }
        public static int EncodeNext(AVCodecContext* avctx, AVPacket* avpkt, AVFrame* frame, ref int got_packet_ptr)
        {
            int ret = 0;
            got_packet_ptr = 0;
            if ((ret = ffmpeg.avcodec_receive_packet(avctx, avpkt)) == 0)
            {
                got_packet_ptr = 1;
                //0 on success, otherwise negative error code
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                //output is not available in the current state - user must try to send input
                return Encode(avctx, avpkt, frame, ref got_packet_ptr);
            }
            else if (ret == ffmpeg.AVERROR_EOF)
            {
                die("AVERROR_EOF: the encoder has been fully flushed, and there will be no more output packets");
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.EINVAL))
            {
                die("AVERROR(EINVAL) codec not opened, or it is an encoder other errors: legitimate decoding errors");
            }
            else
            {
                die("unknown");
            }
            return ret;//ffmpeg.avcodec_encode_audio2(audioCodecContext, &outPacket, audioFrameConverted, &frameFinished)
        }
        public static int Encode(AVCodecContext* avctx, AVPacket* avpkt, AVFrame* frame, ref int got_packet_ptr)
        {
            int ret = 0;
            got_packet_ptr = 0;
            if ((ret = ffmpeg.avcodec_send_frame(avctx, frame)) == 0)
            {
                //0 on success, otherwise negative error code
                return EncodeNext(avctx, avpkt, frame, ref got_packet_ptr);
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN))
            {
                die("input is not accepted in the current state - user must read output with avcodec_receive_packet() (once all output is read, the packet should be resent, and the call will not fail with EAGAIN)");
            }
            else if (ret == ffmpeg.AVERROR_EOF)
            {
                die("AVERROR_EOF: the decoder has been flushed, and no new packets can be sent to it (also returned if more than 1 flush packet is sent");
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.EINVAL))
            {
                die("AVERROR(ffmpeg.EINVAL) codec not opened, refcounted_frames not set, it is a decoder, or requires flush");
            }
            else if (ret == ffmpeg.AVERROR(ffmpeg.ENOMEM))
            {
                die("AVERROR(ENOMEM) failed to add packet to internal queue, or similar other errors: legitimate decoding errors");
            }
            else
            {
                die("unknown");
            }
            return ret;//ffmpeg.avcodec_encode_audio2(audioCodecContext, &outPacket, audioFrameConverted, &frameFinished)
        }
        public static int EncodeFlush(AVCodecContext* avctx)
        {
            return ffmpeg.avcodec_send_frame(avctx, null);
        }
        public static void Main(string[] argv)
        {
            //ffmpeg.av_register_all();

            if (argv.Length != 2)
            {
                //fprintf(stderr, "%s <in> <out>\n", argv[0]);
                return;
            }

            // Allocate and init re-usable frames
            AVCodecContext* fileCodecContext, audioCodecContext;
            AVFormatContext* formatContext, outContext;
            AVStream* out_audioStream;
            SwrContext* swrContext;
            int streamId;

            // input file
            string file = argv[0];
            int res = ffmpeg.avformat_open_input(&formatContext, file, null, null);
            if (res != 0)
            {
                die("avformat_open_input");
            }

            res = ffmpeg.avformat_find_stream_info(formatContext, null);
            if (res < 0)
            {
                die("avformat_find_stream_info");
            }

            AVCodec* codec;
            res = ffmpeg.av_find_best_stream(formatContext, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, &codec, 0);
            if (res < 0)
            {
                return; // die("av_find_best_stream");
            }

            streamId = res;
            fileCodecContext = ffmpeg.avcodec_alloc_context3(codec);
            AVCodecParameters* cp = null;
            ffmpeg.avcodec_parameters_to_context(fileCodecContext, formatContext->streams[streamId]->codecpar);
            res = ffmpeg.avcodec_open2(fileCodecContext, codec, null);
            if (res < 0)
            {
                die("avcodec_open2");
            }

            in_audioStream = formatContext->streams[streamId];

            // output file
            //string outfile = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(argv[0])}.pcm");
            //AVOutputFormat* fmt = fmt = ffmpeg.av_guess_format("s16le", null, null);
            string outfile = argv[1];
            AVOutputFormat * fmt = fmt = ffmpeg.av_guess_format(null, outfile, null);
            if (fmt == null)
            {
                die("av_guess_format");
            }

            outContext = ffmpeg.avformat_alloc_context();
            outContext->oformat = fmt;
            out_audioStream = add_audio_stream(outContext, fmt->audio_codec, in_audioStream->codec->sample_rate);
            open_audio(outContext, out_audioStream);
            out_audioStream->time_base = in_audioStream->time_base;
            res = ffmpeg.avio_open2(&outContext->pb, outfile, ffmpeg.AVIO_FLAG_WRITE, null, null);
            if (res < 0)
            {
                die("url_fopen");
            }

            ffmpeg.avformat_write_header(outContext, null);
            AVCodec* ocodec;
            res = ffmpeg.av_find_best_stream(outContext, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, &ocodec, 0);
            audioCodecContext = ffmpeg.avcodec_alloc_context3(ocodec);
            ffmpeg.avcodec_parameters_to_context(audioCodecContext, out_audioStream->codecpar);
            res = ffmpeg.avcodec_open2(audioCodecContext, ocodec, null);
            if (res < 0)
            {
                die("avcodec_open2");
            }
            // resampling
            swrContext = ffmpeg.swr_alloc();
            ffmpeg.av_opt_set_channel_layout(swrContext, "in_channel_layout", (long)fileCodecContext->channel_layout, 0);
            ffmpeg.av_opt_set_channel_layout(swrContext, "out_channel_layout", (long)audioCodecContext->channel_layout, 0);
            ffmpeg.av_opt_set_int(swrContext, "in_sample_rate", fileCodecContext->sample_rate, 0);
            ffmpeg.av_opt_set_int(swrContext, "out_sample_rate", audioCodecContext->sample_rate, 0);
            ffmpeg.av_opt_set_sample_fmt(swrContext, "in_sample_fmt", fileCodecContext->sample_fmt, 0);
            ffmpeg.av_opt_set_sample_fmt(swrContext, "out_sample_fmt", audioCodecContext->sample_fmt, 0);
            res = ffmpeg.swr_init(swrContext);
            if (res < 0)
            {
                die("swr_init");
            }

            AVFrame* audioFrameDecoded = ffmpeg.av_frame_alloc();
            if (audioFrameDecoded == null)
            {
                die("Could not allocate audio frame");
            }

            audioFrameDecoded->format = (int)fileCodecContext->sample_fmt;
            audioFrameDecoded->channel_layout = fileCodecContext->channel_layout;
            audioFrameDecoded->channels = fileCodecContext->channels;
            audioFrameDecoded->sample_rate = fileCodecContext->sample_rate;

            AVFrame* audioFrameConverted = ffmpeg.av_frame_alloc();
            if (audioFrameConverted == null)
            {
                die("Could not allocate audio frame");
            }

            audioFrameConverted->nb_samples = audioCodecContext->frame_size;
            audioFrameConverted->format = (int)audioCodecContext->sample_fmt;
            audioFrameConverted->channel_layout = audioCodecContext->channel_layout;
            audioFrameConverted->channels = audioCodecContext->channels;
            audioFrameConverted->sample_rate = audioCodecContext->sample_rate;
            if (audioFrameConverted->nb_samples <= 0)
            {
                audioFrameConverted->nb_samples = 32;
            }

            AVPacket inPacket;
            ffmpeg.av_init_packet(&inPacket);
            inPacket.data = null;
            inPacket.size = 0;

            int frameFinished = 0;


            for (; ; )
            {
                if (ffmpeg.av_read_frame(formatContext, &inPacket) < 0)
                {
                    break;
                }

                if (inPacket.stream_index == streamId)
                {
                    int len = Decode(fileCodecContext, audioFrameDecoded, ref frameFinished, &inPacket);
                    if (len == ffmpeg.AVERROR_EOF)
                    {
                        break;
                    }

                    if (frameFinished != 0)
                    {

                        // Convert

                        byte* convertedData = null;

                        if (ffmpeg.av_samples_alloc(&convertedData,
                                     null,
                                     audioCodecContext->channels,
                                     audioFrameConverted->nb_samples,
                                     audioCodecContext->sample_fmt, 0) < 0)
                        {
                            die("Could not allocate samples");
                        }

                        int outSamples = 0;
                        fixed (byte** tmp = (byte*[])audioFrameDecoded->data)
                        {
                            outSamples = ffmpeg.swr_convert(swrContext, null, 0,
                                         //&convertedData,
                                         //audioFrameConverted->nb_samples,
                                         tmp,
                                 audioFrameDecoded->nb_samples);
                        }
                        if (outSamples < 0)
                        {
                            die("Could not convert");
                        }

                        for (; ; )
                        {
                            outSamples = ffmpeg.swr_get_out_samples(swrContext, 0);
                            if ((outSamples < audioCodecContext->frame_size * audioCodecContext->channels) || audioCodecContext->frame_size == 0 && (outSamples < audioFrameConverted->nb_samples * audioCodecContext->channels))
                            {
                                break; // see comments, thanks to @dajuric for fixing this
                            }

                            outSamples = ffmpeg.swr_convert(swrContext,
                                                     &convertedData,
                                                     audioFrameConverted->nb_samples, null, 0);

                            int buffer_size = ffmpeg.av_samples_get_buffer_size(null,
                                           audioCodecContext->channels,
                                           audioFrameConverted->nb_samples,
                                           audioCodecContext->sample_fmt,
                                           0);
                            if (buffer_size < 0)
                            {
                                die("Invalid buffer size");
                            }

                            if (ffmpeg.avcodec_fill_audio_frame(audioFrameConverted,
                                     audioCodecContext->channels,
                                     audioCodecContext->sample_fmt,
                                     convertedData,
                                     buffer_size,
                                     0) < 0)
                            {
                                die("Could not fill frame");
                            }

                            AVPacket outPacket;
                            ffmpeg.av_init_packet(&outPacket);
                            outPacket.data = null;
                            outPacket.size = 0;
                            if (Encode(audioCodecContext, &outPacket, audioFrameConverted, ref frameFinished) < 0)
                            {
                                die("Error encoding audio frame");
                            }


                            //outPacket.flags |= ffmpeg.AV_PKT_FLAG_KEY;
                            outPacket.stream_index = out_audioStream->index;
                            //outPacket.data = audio_outbuf;
                            outPacket.dts = audioFrameDecoded->pkt_dts;
                            outPacket.pts = audioFrameDecoded->pkt_pts;
                            ffmpeg.av_packet_rescale_ts(&outPacket, in_audioStream->time_base, out_audioStream->time_base);

                            if (frameFinished != 0)
                            {


                                if (ffmpeg.av_interleaved_write_frame(outContext, &outPacket) != 0)
                                {
                                    die("Error while writing audio frame");
                                }

                                ffmpeg.av_packet_unref(&outPacket);
                            }
                        }
                    }
                }
            }
            EncodeFlush(audioCodecContext);
            DecodeFlush(fileCodecContext, &inPacket);

            ffmpeg.swr_close(swrContext);
            ffmpeg.swr_free(&swrContext);
            ffmpeg.av_frame_free(&audioFrameConverted);
            ffmpeg.av_frame_free(&audioFrameDecoded);
            ffmpeg.av_packet_unref(&inPacket);
            ffmpeg.av_write_trailer(outContext);
            ffmpeg.avio_close(outContext->pb);
            ffmpeg.avcodec_close(fileCodecContext);
            ffmpeg.avcodec_free_context(&fileCodecContext);
            ffmpeg.avformat_close_input(&formatContext);
            return;
        }
    }
}