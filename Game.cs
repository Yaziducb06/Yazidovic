﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Lines
{
    internal class Game
    {
        int max;
        int[,] map = new int[8, 8]; //0 - пусто, 1 - 6 шарик цвета N
        int max_colors = 6;
        ShowItem Show;
        Status status;
        Ball[] ball = new Ball[3];
        static Random rand = new Random();
        public int score = 0;
        enum Status
        {
            init, // самое начало
            wait, // ожидание выбор первого шарика
            ball_mark, // шарик выбром - отмечен - ожидаем выбор точки
            path_show, // показать путь сле шарика
            ball_move, // процесс перемешения шарика
            next_balls, // вывод подсказки по след шарикам
            line_strip, //"вырыв" собранник линий
            stop // поле заполнено, конец игры
        }

        public Game(int max, ShowItem show)
        {
            this.max = max;
            this.Show = show;
            map = new int[max, max];
            fmap = new int[max, max];
            status = Status.init;
            path = new Ball[81];
            strip = new Ball[99];
        }
        private void InitMap()
        {
            Ball none;
            none.color = 0;
            score = 0;
            for(int x = 0; x < max; x++)
                for(int y = 0;y < max;y++)
                {
                    map[x, y] = 0;
                    none.x = x;
                    none.y = y;
                    Show(none,Item.none,score);
                }
        }
        Ball marked_ball;
        Ball destin_ball;
        int marked_jump;
        public void ClickBox(int x, int y)
        {
            if(status == Status.wait || status == Status.ball_mark)
            {
                if (map[x, y] > 0)
                {   if (status == Status.ball_mark)
                        Show(marked_ball, Item.ball,score);
                    marked_ball.x = x;
                    marked_ball.y = y;
                    marked_ball.color = map[x,y];
                    status = Status.ball_mark;
                    marked_jump = 0;
                    return;
                }
            }
            if(status == Status.ball_mark)
                if (map[x, y] <= 0)
                {
                    destin_ball.x = x;
                    destin_ball.y = y;
                    destin_ball.color = marked_ball.color;
                    if(FindPath())
                        status = Status.path_show;
                    return;
                }
            if(status == Status.stop)
                status = Status.init;
        }
        public void step()
        {
            switch (status)
            {
                case Status.init:
                    InitMap();
                    SelectNextBalls();
                    ShowNextBalls();
                    SelectNextBalls();
                    status = Status.wait;
                    break;
                case Status.wait:
                    break;
                case Status.ball_mark:
                    JumpBall();
                    break;
                case Status.path_show:
                    PathShow();
                    break;
                case Status.ball_move:
                    MoveBall();
                    break;
                case Status.line_strip:
                    StrioLines();
                    break;
                case Status.next_balls:
                    ShowNextBalls();
                    SelectNextBalls();
                    break;
                case Status.stop:
                    break;

            }
        }
        public void SelectNextBalls()
        {
            ball[0] = SelectNextBall();
            ball[1] = SelectNextBall();
            ball[2] = SelectNextBall();
        }
        private Ball SelectNextBall()
        {
            return SelectNextBall(rand.Next(1, max_colors));
        }
            private Ball SelectNextBall(int color)
        {
            int loop = 100;
            Ball next;
            next.color = color;
            do
            {
                next.x = rand.Next(0, max);
                next.y = rand.Next(0, max);
                if(--loop < 0)
                {
                    next.x = -1;
                    return next;
                }
            } while (map[next.x, next.y] != 0);
            map[next.x, next.y] = -1;
            Show(next, Item.next,score);
            return next;
        }
        private void ShowNextBalls()
        {
            ShowNextBall(ball[0]);
            ShowNextBall(ball[1]);
            ShowNextBall(ball[2]);
            if(FindStripLines())
                status = Status.line_strip;
            else
                if(IsMapFull())
                    status = Status.stop;
                else
                    status = Status.wait;
        }
        private void ShowNextBall(Ball next)
        {
            if (next.x < 0) return;
            if (map[next.x,next.y] > 0)
            {
                next = SelectNextBall(next.color);
                if (next.x < 0) return;
            }
            map[next.x,next.y] = next.color;
            Show(next, Item.ball,score);
        }
        private void JumpBall()
        {   if (status != Status.ball_mark)
                return;
            if (marked_jump == 0)
            Show(marked_ball,Item.jump,score);
            else
            Show(marked_ball,Item.ball,score);
            marked_jump = 1 - marked_jump;
        }
        private void MoveBall()
        {
            if (status != Status.ball_move)
                return;
            if (map[marked_ball.x,marked_ball.y] > 0 && map[destin_ball.x,destin_ball.y] <=0 )
            {
                map[marked_ball.x, marked_ball.y] = 0;
                map[destin_ball.x, destin_ball.y] = marked_ball.color;
                Show(marked_ball, Item.none,score);
                Show(destin_ball, Item.ball,score);
               if(FindStripLines())
                    status = Status.line_strip;
               else
                    status = Status.next_balls;
            }
        }
        int[,] fmap;
        Ball[] path;
        int paths;
        private bool FindPath()
        {
            if (!(map[marked_ball.x, marked_ball.y] > 0 && map[destin_ball.x, destin_ball.y] <= 0))
                return false;
            for (int x = 0; x < max; x++)
                for(int y = 0; y < max; y++)
                    fmap[x, y] = 0;
            bool added;
            bool found = false;
            fmap[marked_ball.x, marked_ball.y] = 1;
            int nr = 1;
            do
            {
                added = false;
                for (int x = 0; x < max; x++)
                    for (int y = 0; y < max; y++)
                        if (fmap[x, y] == nr)
                        {
                            MarkPath(x + 1 , y,nr +1);
                            MarkPath(x - 1 , y,nr + 1);
                            MarkPath(x , y + 1,nr + 1);
                            MarkPath(x , y - 1,nr + 1);
                            added = true;
                        }
                if (fmap[destin_ball.x,destin_ball.y] > 0)
                {
                    found = true;
                    break;
                }
                nr++;
            } while (added);
            if(!found) 
                return false;
            int px = destin_ball.x;
            int py = destin_ball.y;

            paths = nr;

            while (nr >= 0) 
            {
                path[nr].x = px;
                path[nr].y = py;
                if (IsPath(px + 1, py, nr)) px++;
                else if (IsPath(px - 1, py, nr)) px--;
                else if (IsPath(px, py + 1, nr)) py++;
                else if(IsPath(px,py - 1, nr)) py--;
                nr--;
            }
            path_step = 0;
            return true;
        }
        private void MarkPath(int x, int y,int k)
        {
            if(x < 0 || x >= max) return;
            if(y < 0 || y >= max) return;
            if (map[x, y] > 0) return;
            if (fmap[x,y] > 0) return;
            fmap[x, y ] = k;
        }

        private bool IsPath(int x,int y,int k)
        {
            if (x < 0 || x >= max) return false;
            if(y < 0 || y >= max) return false;
            return (fmap[x,y] == k);
        }
        int path_step;
        private void PathShow()
        {
            if(path_step == 0)
            {
                for (int nr = 1; nr <= paths; nr++)
                    Show(path[nr], Item.path,score);
                path_step++;
                return;
            }
            Ball moving_ball;

            moving_ball = path[path_step - 1];
            Show(moving_ball, Item.none,score);

            moving_ball = path[path_step];
            moving_ball.color = marked_ball.color;
            Show(moving_ball, Item.ball,score);

            path_step++;

            if (path_step > paths)
                status = Status.ball_move;
        }
        Ball [] strip;
        int strips;
        int strip_step;
        private bool FindStripLines()
        {
            strips = 0;
            for(int x = 0;x < max ; x++)
            {
                for(int y = 0;y < max; y++)
                {
                    CheckLine(x, y, 1, 0);
                    CheckLine(x, y, 1, 1);
                    CheckLine(x, y, 0, 1);
                    CheckLine(x, y, -1, 1);
                }
            }
            if(strips == 0)
                return false;
            strip_step = 4;
            return true;
        }
        private void CheckLine(int x,int y,int sx,int sy)
        {
            int p = 4;
            if(x < 0 || x >= max) return;
            if(y < 0 || y >= max) return;
            if(x + p*sx < 0 || x + p*sx >= max) return;
            if(y + p*sy < 0 || y + p*sy >= max) return;
            int color = map[x, y];
            if(color <= 0)
                return;
            for(int k = 1; k <= p; k++)
                if(map[x + k * sx,y + k * sy] != color)
                    return;
            for(int k = 0; k <= p; k++)
            {
                strip[strips].x = x + k*sx;
                strip[strips].y = y + k*sy;
                strip[strips].color = color;
                if (strips >= 5)
                {
                    for (int i = 0; i < 5; i++)
                        if (strip[strips].x == strip[i].x || strip[strips].y == strip[i].y)
                            score--;
                }
                else
                    score++;
                strips++;
            }            
        }
        private void StrioLines()
        {
           if(strip_step <= 0)
            {
                for(int j = 0; j < strips ; j++)
                    map[strip[j].x,strip[j].y] = 0;
                ShowNextBalls();
                SelectNextBalls();
                status = Status.wait;
                return;
            }
            strip_step--;
            for(int j = 0;j < strips;j++)
            {
                switch(strip_step)
                {
                    case 3: Show(strip[j],Item.jump,score);break;
                    case 2: Show(strip[j],Item.ball,score);break;
                    case 1: Show(strip[j],Item.next,score);break;
                    case 0: Show(strip[j],Item.none,score);break;
                }
            }
            
        }
        private bool IsMapFull()
        {
            for(int x = 0; x < max; x++)
                for(int y = 0; y  < max; y++)
                    if(map[x, y] <= 0)
                        return false;
            return true;
        }
    }
}
